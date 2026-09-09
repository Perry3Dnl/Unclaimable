using System.Globalization;
using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private bool TryFindFirstPolicyViolation(string? value, out Result? violation)
    {
        violation = null;
        if (value is null)
        {
            return false;
        }

        if (TryFindMalformedUtf16(value, out var invalidIndex, out var invalidCharacter))
        {
            violation = Result.InvalidCharacters(value, invalidIndex, invalidCharacter);
            return true;
        }

        if ((_rejectInvisibleOnlyIdentifiers || _rejectControlCharacters || _rejectFormatCharacters)
            && TryFindFirstUnicodeStrictnessViolation(value, out violation))
        {
            return true;
        }

        if (_minimumLengthEnabled && value.Length < _minimumLength)
        {
            violation = Result.TooShort(value, _minimumLength);
            return true;
        }

        if (_maximumLengthEnabled && value.Length > _maximumLength)
        {
            violation = Result.TooLong(value, _maximumLength);
            return true;
        }

        if (value.Length == 0)
        {
            return false;
        }

        if (_leadingSeparatorEnabled && IsSeparator(value[0]))
        {
            violation = Result.LeadingSeparator(value, 0, value[0].ToString());
            return true;
        }

        if (_trailingSeparatorEnabled && IsSeparator(value[value.Length - 1]))
        {
            violation = Result.TrailingSeparator(value, value.Length - 1, value[value.Length - 1].ToString());
            return true;
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var characterText = character.ToString();
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                characterText = new string(new[] { character, value[index + 1] });
            }

            if (!_allowNumbers && category == UnicodeCategory.DecimalDigitNumber)
            {
                violation = Result.NumbersNotAllowed(value, index, characterText);
                return true;
            }

            if (_asciiOnly && (character < 0x20 || character > 0x7E))
            {
                violation = Result.InvalidCharacters(value, index, characterText);
                return true;
            }

            var explicitlyAllowed = _policy.IsCharacterExplicitlyAllowed(characterText);
            if (!explicitlyAllowed && _whitespaceEnabled && IsWhitespace(category, character))
            {
                violation = Result.BlockedCharacter(value, index, characterText);
                return true;
            }

            if (!explicitlyAllowed && _blockedCharactersEnabled && _policy.IsCharacterBlocked(characterText))
            {
                violation = Result.BlockedCharacter(value, index, characterText);
                return true;
            }

            if (characterText.Length == 2)
            {
                index++;
            }
        }

        return false;
    }

    private bool TryFindFirstUnicodeStrictnessViolation(string value, out Result? violation)
    {
        violation = null;
        var hasVisibleContent = false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var characterText = character.ToString();
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                characterText = new string(new[] { character, value[index + 1] });
            }

            if (_rejectControlCharacters && category == UnicodeCategory.Control)
            {
                violation = Result.InvalidCharacters(value, index, characterText);
                return true;
            }

            if (_rejectFormatCharacters && category == UnicodeCategory.Format)
            {
                violation = Result.InvalidCharacters(value, index, characterText);
                return true;
            }

            if (!IsInvisibleForApproximation(category, character))
            {
                hasVisibleContent = true;
            }

            if (characterText.Length == 2)
            {
                index++;
            }
        }

        if (_rejectInvisibleOnlyIdentifiers && value.Length > 0 && !hasVisibleContent)
        {
            violation = Result.InvalidCharacters(value);
            return true;
        }

        return false;
    }

    private void CollectPolicyDiagnostics(
        string? value,
        bool includeMessages,
        List<Diagnostic> diagnostics)
    {
        if (value is null)
        {
            return;
        }

        if (_minimumLengthEnabled && value.Length < _minimumLength)
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.TooShort,
                message: includeMessages ? $"Value must be at least {_minimumLength} characters long." : null));
        }

        if (_maximumLengthEnabled && value.Length > _maximumLength)
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.TooLong,
                message: includeMessages ? $"Value must be no more than {_maximumLength} characters long." : null));
        }

        if (value.Length == 0)
        {
            return;
        }

        if (_leadingSeparatorEnabled && IsSeparator(value[0]))
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.LeadingSeparator,
                offendingCharacterIndex: 0,
                offendingCharacter: value[0].ToString(),
                message: includeMessages ? "Leading separators are not allowed." : null));
        }

        if (_trailingSeparatorEnabled && IsSeparator(value[value.Length - 1]))
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.TrailingSeparator,
                offendingCharacterIndex: value.Length - 1,
                offendingCharacter: value[value.Length - 1].ToString(),
                message: includeMessages ? "Trailing separators are not allowed." : null));
        }

        var hasVisibleContent = false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var characterText = character.ToString();
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                characterText = new string(new[] { character, value[index + 1] });
            }

            if (!IsInvisibleForApproximation(category, character))
            {
                hasVisibleContent = true;
            }

            if (_rejectControlCharacters && category == UnicodeCategory.Control)
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.InvalidCharacters,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages
                        ? $"Control character '{characterText}' at index {index} is not allowed."
                        : null));
            }

            if (_rejectFormatCharacters && category == UnicodeCategory.Format)
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.InvalidCharacters,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages
                        ? $"Formatting character '{characterText}' at index {index} is not allowed."
                        : null));
            }

            if (!_allowNumbers && category == UnicodeCategory.DecimalDigitNumber)
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.NumbersNotAllowed,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages
                        ? $"Numbers are not allowed; '{characterText}' at index {index} is not permitted."
                        : null));
            }

            if (_asciiOnly && (character < 0x20 || character > 0x7E))
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.InvalidCharacters,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages
                        ? $"Character '{characterText}' at index {index} is not allowed by the ASCII-only policy."
                        : null));
            }

            var explicitlyAllowed = _policy.IsCharacterExplicitlyAllowed(characterText);
            if (!explicitlyAllowed && _whitespaceEnabled && IsWhitespace(category, character))
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.BlockedCharacter,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages ? $"Character '{characterText}' at index {index} is blocked." : null));
            }
            else if (!explicitlyAllowed && _blockedCharactersEnabled && _policy.IsCharacterBlocked(characterText))
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.BlockedCharacter,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages ? $"Character '{characterText}' at index {index} is blocked." : null));
            }

            if (characterText.Length == 2)
            {
                index++;
            }
        }

        if (_rejectInvisibleOnlyIdentifiers && !hasVisibleContent)
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.InvalidCharacters,
                message: includeMessages
                    ? "Value does not contain a Unicode scalar outside whitespace, control, format, or combining-mark categories."
                    : null));
        }
    }

    private static Diagnostic ToDiagnostic(Result result, bool includeMessage)
    {
        return new Diagnostic(
            result.MatchKind,
            result.MatchedValue,
            result.Category,
            result.OffendingCharacterIndex,
            result.OffendingCharacter,
            result.MatchStartIndex,
            result.MatchLength,
            includeMessage ? BuildMessage(result) : null,
            result.OriginalMatchStartIndex,
            result.OriginalMatchLength);
    }

    private static string BuildMessage(Result result)
    {
        switch (result.MatchKind)
        {
            case MatchKind.Exact:
                return $"'{result.MatchedValue}' is reserved and cannot be claimed.";
            case MatchKind.Compact:
                return $"This value resolves to the reserved value '{result.MatchedValue}' after separators or punctuation are ignored.";
            case MatchKind.Partial:
                return $"This value contains the reserved value '{result.MatchedValue}'.";
            case MatchKind.Obfuscated:
                return $"This value appears to obfuscate the reserved value '{result.MatchedValue}'.";
            case MatchKind.UnicodeConfusable:
                return $"This value contains Unicode lookalikes that resolve to the reserved value '{result.MatchedValue}'.";
            case MatchKind.NumbersNotAllowed:
                return $"Numbers are not allowed; '{result.OffendingCharacter}' at index {result.OffendingCharacterIndex} is not permitted.";
            case MatchKind.InvalidCharacters:
                return $"Character '{result.OffendingCharacter}' at index {result.OffendingCharacterIndex} is not allowed.";
            case MatchKind.TooShort:
                return "This value is shorter than the configured minimum length.";
            case MatchKind.TooLong:
                return "This value is longer than the configured maximum length.";
            case MatchKind.BlockedCharacter:
                return $"Character '{result.OffendingCharacter}' at index {result.OffendingCharacterIndex} is blocked.";
            case MatchKind.LeadingSeparator:
                return "Leading separators are not allowed.";
            case MatchKind.TrailingSeparator:
                return "Trailing separators are not allowed.";
            default:
                return "This value is not allowed.";
        }
    }
}
