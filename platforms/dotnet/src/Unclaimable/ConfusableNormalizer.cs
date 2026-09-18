using System.Globalization;
using System.Text;

namespace Unclaimable;

/// <summary>Shared internal normalization for the selected confusable and obfuscation mappings used by Unclaimable packages.</summary>
internal static class ConfusableNormalizer
{
    internal static string CreateSkeleton(
        string value,
        bool includeAsciiObfuscation,
        out bool changed)
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
            if (TryMapUnicodeConfusable(character, out mapped)
                || (includeAsciiObfuscation && TryMapAsciiDomainConfusable(character, out mapped)))
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

    internal static bool TryGetObfuscationSubstitutions(char character, out string[]? substitutions)
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

    private static bool TryMapAsciiDomainConfusable(char character, out char mapped)
    {
        if (character == '1')
        {
            mapped = 'l';
            return true;
        }

        string[]? substitutions;
        if (TryGetObfuscationSubstitutions(character, out substitutions)
            && substitutions!.Length == 1
            && substitutions[0].Length == 1)
        {
            mapped = substitutions[0][0];
            return true;
        }

        mapped = (char)0;
        return false;
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
            case (char)0x03C2: mapped = 'c'; return true;
            case (char)0x0131: mapped = 'i'; return true;
            default:
                mapped = (char)0;
                return false;
        }
    }
}
