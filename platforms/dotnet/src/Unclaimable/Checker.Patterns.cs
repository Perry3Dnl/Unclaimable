using System.Globalization;
using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private const int MinimumRepeatedPatternElements = 8;
    private const int MinimumPatternRepetitions = 4;
    private const int MaximumRepeatedUnitElements = 4;

    private readonly Pattern _enabledPatterns;

    private bool TryFindFirstPatternViolation(string? value, out Result? violation)
    {
        violation = null;
        var patternText = NormalizePatternText(value);
        if (patternText is null)
        {
            return false;
        }

        if (IsPatternEnabled(Pattern.NumericOnly) && IsNumericOnlyPattern(patternText))
        {
            violation = CreatePatternResult(value!, MatchKind.NumericOnly);
            return true;
        }

        if (IsPatternEnabled(Pattern.AsciiArt) && IsAsciiArtPattern(patternText))
        {
            violation = CreatePatternResult(value!, MatchKind.AsciiArt);
            return true;
        }

        if (IsPatternEnabled(Pattern.SymbolOnly) && IsSymbolOnlyPattern(patternText))
        {
            violation = CreatePatternResult(value!, MatchKind.SymbolOnly);
            return true;
        }

        if (IsPatternEnabled(Pattern.Repeated) && IsRepeatedPattern(patternText))
        {
            violation = CreatePatternResult(value!, MatchKind.RepeatedPattern);
            return true;
        }

        if (IsPatternEnabled(Pattern.UppercaseOnly) && IsUppercaseOnlyPattern(patternText))
        {
            violation = CreatePatternResult(value!, MatchKind.UppercaseOnly);
            return true;
        }

        return false;
    }

    private void CollectPatternDiagnostics(
        string? value,
        bool includeMessages,
        List<Diagnostic> diagnostics)
    {
        var patternText = NormalizePatternText(value);
        if (patternText is null)
        {
            return;
        }

        if (IsPatternEnabled(Pattern.NumericOnly) && IsNumericOnlyPattern(patternText))
        {
            AddPatternDiagnostic(MatchKind.NumericOnly, includeMessages, diagnostics);
        }

        if (IsPatternEnabled(Pattern.AsciiArt) && IsAsciiArtPattern(patternText))
        {
            AddPatternDiagnostic(MatchKind.AsciiArt, includeMessages, diagnostics);
        }

        if (IsPatternEnabled(Pattern.SymbolOnly) && IsSymbolOnlyPattern(patternText))
        {
            AddPatternDiagnostic(MatchKind.SymbolOnly, includeMessages, diagnostics);
        }

        if (IsPatternEnabled(Pattern.Repeated) && IsRepeatedPattern(patternText))
        {
            AddPatternDiagnostic(MatchKind.RepeatedPattern, includeMessages, diagnostics);
        }

        if (IsPatternEnabled(Pattern.UppercaseOnly) && IsUppercaseOnlyPattern(patternText))
        {
            AddPatternDiagnostic(MatchKind.UppercaseOnly, includeMessages, diagnostics);
        }
    }

    private bool IsPatternEnabled(Pattern pattern) => (_enabledPatterns & pattern) == pattern;

    private static string? NormalizePatternText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Normalize(NormalizationForm.FormKC);
    }

    private static bool IsNumericOnlyPattern(string value)
    {
        var sawDigit = false;

        for (var index = 0; index < value.Length; index++)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            if (category != UnicodeCategory.DecimalDigitNumber)
            {
                return false;
            }

            sawDigit = true;
            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
            }
        }

        return sawDigit;
    }

    private static bool IsSymbolOnlyPattern(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            if (IsPatternLetterOrNumber(category))
            {
                return false;
            }

            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
            }
        }

        return true;
    }

    private static bool IsPatternLetterOrNumber(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter
               || category == UnicodeCategory.LowercaseLetter
               || category == UnicodeCategory.TitlecaseLetter
               || category == UnicodeCategory.ModifierLetter
               || category == UnicodeCategory.OtherLetter
               || category == UnicodeCategory.DecimalDigitNumber
               || category == UnicodeCategory.LetterNumber
               || category == UnicodeCategory.OtherNumber;
    }

    private static bool IsRepeatedPattern(string value)
    {
        var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(value.ToLowerInvariant());

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.GetTextElement());
        }

        if (elements.Count < MinimumRepeatedPatternElements)
        {
            return false;
        }

        var maximumUnitLength = Math.Min(MaximumRepeatedUnitElements, elements.Count / MinimumPatternRepetitions);

        for (var unitLength = 1; unitLength <= maximumUnitLength; unitLength++)
        {
            if (elements.Count % unitLength != 0
                || elements.Count / unitLength < MinimumPatternRepetitions)
            {
                continue;
            }

            var repeated = true;
            for (var index = unitLength; index < elements.Count; index++)
            {
                if (!string.Equals(elements[index], elements[index % unitLength], StringComparison.Ordinal))
                {
                    repeated = false;
                    break;
                }
            }

            if (repeated)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsUppercaseOnlyPattern(string value)
    {
        var sawCasedLetter = false;

        for (var index = 0; index < value.Length; index++)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);

            if (category == UnicodeCategory.LowercaseLetter
                || category == UnicodeCategory.TitlecaseLetter)
            {
                return false;
            }

            if (category == UnicodeCategory.UppercaseLetter)
            {
                sawCasedLetter = true;
            }

            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
            }
        }

        return sawCasedLetter;
    }

    private static bool IsAsciiArtPattern(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        var compact = builder.ToString();
        if (compact.Length == 0)
        {
            return false;
        }

        if (LooksLikePhallicAsciiArt(compact))
        {
            return true;
        }

        return string.Equals(compact, "(.)(.)", StringComparison.OrdinalIgnoreCase)
               || string.Equals(compact, "(o)(o)", StringComparison.OrdinalIgnoreCase)
               || string.Equals(compact, "(@)(@)", StringComparison.OrdinalIgnoreCase)
               || string.Equals(compact, "(_!_)", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikePhallicAsciiArt(string value)
    {
        if (value.Length < 5)
        {
            return false;
        }

        var first = char.ToUpperInvariant(value[0]);
        var last = char.ToUpperInvariant(value[value.Length - 1]);

        if ((first != '8' && first != 'B')
            || (last != '3' && last != 'D'))
        {
            return false;
        }

        if (value.Length - 2 < 3)
        {
            return false;
        }

        for (var index = 1; index < value.Length - 1; index++)
        {
            var character = value[index];
            if (character != '=' && character != '-' && character != '~')
            {
                return false;
            }
        }

        return true;
    }

    private static Result CreatePatternResult(string input, MatchKind kind) =>
        new Result(true, input, null, null, kind);

    private static void AddPatternDiagnostic(
        MatchKind kind,
        bool includeMessages,
        List<Diagnostic> diagnostics)
    {
        diagnostics.Add(new Diagnostic(
            kind,
            message: includeMessages ? GetPatternMessage(kind) : null));
    }

    private static string GetPatternMessage(MatchKind kind)
    {
        switch (kind)
        {
            case MatchKind.NumericOnly:
                return "Value cannot consist only of numbers.";
            case MatchKind.RepeatedPattern:
                return "Value cannot consist of a repeated short pattern.";
            case MatchKind.SymbolOnly:
                return "Value must contain at least one letter or number.";
            case MatchKind.AsciiArt:
                return "Value matches a disallowed ASCII-art pattern.";
            case MatchKind.UppercaseOnly:
                return "Value cannot consist only of uppercase letters.";
            default:
                return "Value matches a disallowed identifier pattern.";
        }
    }
}
