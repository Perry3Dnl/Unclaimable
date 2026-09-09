using System.Globalization;
using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private static bool TryFindMalformedUtf16(string value, out int index, out string character)
    {
        for (var current = 0; current < value.Length; current++)
        {
            var codeUnit = value[current];

            if (char.IsHighSurrogate(codeUnit))
            {
                if (current + 1 < value.Length && char.IsLowSurrogate(value[current + 1]))
                {
                    current++;
                    continue;
                }

                index = current;
                character = codeUnit.ToString();
                return true;
            }

            if (char.IsLowSurrogate(codeUnit))
            {
                index = current;
                character = codeUnit.ToString();
                return true;
            }
        }

        index = -1;
        character = string.Empty;
        return false;
    }

    private static InputMapping? TryCreateInputMapping(string input, string exact)
    {
        // The overwhelmingly common identifier path is ASCII with no trimming or
        // compatibility-normalization expansion. Build its positional maps directly
        // instead of allocating and normalizing one temporary string per code unit.
        if (input.Length == exact.Length)
        {
            var simpleAscii = true;
            var compactLength = 0;

            for (var index = 0; index < input.Length; index++)
            {
                var character = input[index];
                if (character > 0x7F || char.ToLowerInvariant(character) != exact[index])
                {
                    simpleAscii = false;
                    break;
                }

                if (char.IsLetterOrDigit(character))
                {
                    compactLength++;
                }
            }

            if (simpleAscii)
            {
                var exactToOriginal = new int[input.Length];
                var compactToOriginal = new int[compactLength];
                var compactIndex = 0;

                for (var index = 0; index < input.Length; index++)
                {
                    exactToOriginal[index] = index;
                    if (char.IsLetterOrDigit(input[index]))
                    {
                        compactToOriginal[compactIndex++] = index;
                    }
                }

                return new InputMapping(exactToOriginal, compactToOriginal);
            }
        }

        var trimStart = 0;
        while (trimStart < input.Length && char.IsWhiteSpace(input[trimStart]))
        {
            trimStart++;
        }

        var trimEnd = input.Length;
        while (trimEnd > trimStart && char.IsWhiteSpace(input[trimEnd - 1]))
        {
            trimEnd--;
        }

        var exactBuilder = new StringBuilder(trimEnd - trimStart);
        var exactToOriginalFallback = new List<int>(trimEnd - trimStart);

        for (var index = trimStart; index < trimEnd; index++)
        {
            var codeUnit = input[index];
            string scalarText;
            var scalarLength = 1;

            if (char.IsHighSurrogate(codeUnit)
                && index + 1 < trimEnd
                && char.IsLowSurrogate(input[index + 1]))
            {
                scalarText = new string(new[] { codeUnit, input[index + 1] });
                scalarLength = 2;
            }
            else
            {
                scalarText = codeUnit.ToString();
            }

            var normalizedScalar = scalarText.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
            if (normalizedScalar.Length != scalarLength)
            {
                return null;
            }

            exactBuilder.Append(normalizedScalar);
            for (var offset = 0; offset < scalarLength; offset++)
            {
                exactToOriginalFallback.Add(index + offset);
            }

            if (scalarLength == 2)
            {
                index++;
            }
        }

        if (!string.Equals(exactBuilder.ToString(), exact, StringComparison.Ordinal))
        {
            return null;
        }

        var compactToOriginalFallback = new List<int>();
        for (var index = 0; index < exact.Length; index++)
        {
            var character = exact[index];

            if (char.IsHighSurrogate(character)
                && index + 1 < exact.Length
                && char.IsLowSurrogate(exact[index + 1]))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(exact, index);
                if (IsLetterOrDigit(category))
                {
                    compactToOriginalFallback.Add(exactToOriginalFallback[index]);
                    compactToOriginalFallback.Add(exactToOriginalFallback[index + 1]);
                }

                index++;
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                compactToOriginalFallback.Add(exactToOriginalFallback[index]);
            }
        }

        return new InputMapping(exactToOriginalFallback.ToArray(), compactToOriginalFallback.ToArray());
    }

    private static bool TryMapOriginalSpan(
        int[]? transformedToOriginal,
        int transformedStart,
        int transformedLength,
        out int? originalStart,
        out int? originalLength)
    {
        originalStart = null;
        originalLength = null;

        if (transformedToOriginal is null
            || transformedLength <= 0
            || transformedStart < 0
            || transformedStart >= transformedToOriginal.Length
            || transformedStart + transformedLength > transformedToOriginal.Length)
        {
            return false;
        }

        var first = transformedToOriginal[transformedStart];
        var last = transformedToOriginal[transformedStart + transformedLength - 1];
        if (last < first)
        {
            return false;
        }

        originalStart = first;
        originalLength = last - first + 1;
        return true;
    }

    private static bool IsSeparator(char character) =>
        character == '-' || character == '_' || character == '.';

    private static bool IsWhitespace(UnicodeCategory category, char character) =>
        char.IsWhiteSpace(character)
        || category == UnicodeCategory.SpaceSeparator
        || category == UnicodeCategory.LineSeparator
        || category == UnicodeCategory.ParagraphSeparator;

    private static bool IsInvisibleForApproximation(UnicodeCategory category, char character) =>
        IsWhitespace(category, character)
        || category == UnicodeCategory.Control
        || category == UnicodeCategory.Format
        || category == UnicodeCategory.NonSpacingMark
        || category == UnicodeCategory.SpacingCombiningMark
        || category == UnicodeCategory.EnclosingMark;

    private static string NormalizeUnicodeConfusables(string value, out bool changed)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        changed = false;

        for (var index = 0; index < decomposed.Length; index++)
        {
            var character = decomposed[index];
            var category = CharUnicodeInfo.GetUnicodeCategory(decomposed, index);

            if (category == UnicodeCategory.NonSpacingMark
                || category == UnicodeCategory.SpacingCombiningMark
                || category == UnicodeCategory.EnclosingMark)
            {
                changed = true;
                continue;
            }

            char mapped;
            if (TryMapUnicodeConfusable(character, out mapped))
            {
                builder.Append(mapped);
                changed = true;
            }
            else
            {
                builder.Append(character);
            }

            if (char.IsHighSurrogate(character)
                && index + 1 < decomposed.Length
                && char.IsLowSurrogate(decomposed[index + 1]))
            {
                builder.Append(decomposed[index + 1]);
                index++;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool TryMapUnicodeConfusable(char character, out char mapped)
    {
        switch (character)
        {
            case (char)0x0430: mapped = 'a'; return true;
            case (char)0x0432: mapped = 'b'; return true;
            case (char)0x0435: mapped = 'e'; return true;
            case (char)0x043A: mapped = 'k'; return true;
            case (char)0x043C: mapped = 'm'; return true;
            case (char)0x043D: mapped = 'h'; return true;
            case (char)0x043E: mapped = 'o'; return true;
            case (char)0x0440: mapped = 'p'; return true;
            case (char)0x0441: mapped = 'c'; return true;
            case (char)0x0442: mapped = 't'; return true;
            case (char)0x0443: mapped = 'y'; return true;
            case (char)0x0445: mapped = 'x'; return true;
            case (char)0x0455: mapped = 's'; return true;
            case (char)0x0456: mapped = 'i'; return true;
            case (char)0x0458: mapped = 'j'; return true;
            case (char)0x04CF: mapped = 'l'; return true;
            case (char)0x03B1: mapped = 'a'; return true;
            case (char)0x03B2: mapped = 'b'; return true;
            case (char)0x03B5: mapped = 'e'; return true;
            case (char)0x03B9: mapped = 'i'; return true;
            case (char)0x03BA: mapped = 'k'; return true;
            case (char)0x03BC: mapped = 'm'; return true;
            case (char)0x03BD: mapped = 'v'; return true;
            case (char)0x03BF: mapped = 'o'; return true;
            case (char)0x03C1: mapped = 'p'; return true;
            case (char)0x03C4: mapped = 't'; return true;
            case (char)0x03C5: mapped = 'y'; return true;
            case (char)0x03C7: mapped = 'x'; return true;
            case (char)0x03F2: mapped = 'c'; return true;
            case (char)0x0131: mapped = 'i'; return true;
            default:
                mapped = (char)0;
                return false;
        }
    }

    private static List<string> ExpandCandidates(List<string> candidates, string[] substitutions)
    {
        var expanded = new List<string>(Math.Min(MaxObfuscationCandidates, candidates.Count * substitutions.Length));

        foreach (var candidate in candidates)
        {
            foreach (var substitution in substitutions)
            {
                if (expanded.Count >= MaxObfuscationCandidates)
                {
                    return expanded;
                }

                expanded.Add(candidate + substitution);
            }
        }

        return expanded;
    }

    private static void AppendToCandidates(List<string> candidates, string value)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            candidates[index] += value;
        }
    }

    private static bool TryGetObfuscationSubstitutions(char character, out string[]? substitutions)
    {
        switch (character)
        {
            case '0': substitutions = new[] { "o" }; return true;
            case '1': substitutions = new[] { "i", "l" }; return true;
            case '2': substitutions = new[] { "z" }; return true;
            case '3': substitutions = new[] { "e" }; return true;
            case '4': substitutions = new[] { "a" }; return true;
            case '5': substitutions = new[] { "s" }; return true;
            case '6':
            case '9': substitutions = new[] { "g" }; return true;
            case '7': substitutions = new[] { "t" }; return true;
            case '8': substitutions = new[] { "b" }; return true;
            case '@': substitutions = new[] { "a" }; return true;
            case '$': substitutions = new[] { "s" }; return true;
            case '!':
            case '|': substitutions = new[] { "i", "l" }; return true;
            case '+': substitutions = new[] { "t" }; return true;
            default:
                substitutions = null;
                return false;
        }
    }
}
