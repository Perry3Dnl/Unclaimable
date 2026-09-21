using System.Globalization;
using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private const int MaximumRepeatedUnitElements = 4;

    private readonly Pattern _enabledPatterns;
    private readonly int _repeatedPatternMinimumLength;

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

        return value!.Trim().Normalize(NormalizationForm.FormKC);
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
        var sawSymbolOrPunctuation = false;

        for (var index = 0; index < value.Length; index++)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
            if (IsPatternLetterOrNumber(category))
            {
                return false;
            }

            if (IsSymbolOrPunctuation(category))
            {
                sawSymbolOrPunctuation = true;
            }

            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
            }
        }

        return sawSymbolOrPunctuation;
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

    private static bool IsSymbolOrPunctuation(UnicodeCategory category)
    {
        return category == UnicodeCategory.ConnectorPunctuation
               || category == UnicodeCategory.DashPunctuation
               || category == UnicodeCategory.OpenPunctuation
               || category == UnicodeCategory.ClosePunctuation
               || category == UnicodeCategory.InitialQuotePunctuation
               || category == UnicodeCategory.FinalQuotePunctuation
               || category == UnicodeCategory.OtherPunctuation
               || category == UnicodeCategory.MathSymbol
               || category == UnicodeCategory.CurrencySymbol
               || category == UnicodeCategory.ModifierSymbol
               || category == UnicodeCategory.OtherSymbol;
    }

    private bool IsRepeatedPattern(string value)
    {
        var elements = new List<string>();
        var enumerator = StringInfo.GetTextElementEnumerator(value.ToLowerInvariant());

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.GetTextElement());
        }

        if (elements.Count < _repeatedPatternMinimumLength)
        {
            return false;
        }

        for (var startIndex = 0; startIndex <= elements.Count - _repeatedPatternMinimumLength; startIndex++)
        {
            var remaining = elements.Count - startIndex;
            var maximumUnitLength = Math.Min(MaximumRepeatedUnitElements, remaining / 2);

            for (var unitLength = 1; unitLength <= maximumUnitLength; unitLength++)
            {
                var matchedLength = unitLength;
                var nextUnitStart = startIndex + unitLength;

                while (nextUnitStart + unitLength <= elements.Count
                       && RepeatedUnitMatches(elements, startIndex, nextUnitStart, unitLength))
                {
                    matchedLength += unitLength;

                    if (matchedLength >= _repeatedPatternMinimumLength)
                    {
                        return true;
                    }

                    nextUnitStart += unitLength;
                }
            }
        }

        return false;
    }

    private static bool RepeatedUnitMatches(
        IReadOnlyList<string> elements,
        int firstUnitStart,
        int candidateUnitStart,
        int unitLength)
    {
        for (var offset = 0; offset < unitLength; offset++)
        {
            if (!string.Equals(
                    elements[firstUnitStart + offset],
                    elements[candidateUnitStart + offset],
                    StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
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
                return "Value cannot contain a repeated short pattern.";
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
