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

}
