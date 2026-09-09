using System.ComponentModel.DataAnnotations;

namespace Unclaimable.AspNetCore;

/// <summary>
/// Validates that a string identifier is claimable according to the registered <see cref="IChecker"/>.
/// Null values are accepted so required-field validation can be handled independently.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ClaimableUsernameAttribute : ValidationAttribute
{
    /// <summary>Validates one value using the registered checker or <see cref="Checker.Default"/>.</summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string text)
        {
            return new ValidationResult($"{validationContext.DisplayName} must be a string.");
        }

        var registeredChecker = validationContext.GetService(typeof(IChecker)) as IChecker;
        var checker = registeredChecker ?? Checker.Default;
        var usingDefaultChecker = registeredChecker is null;

        var result = checker.Check(text);
        if (!result.IsReserved)
        {
            return ValidationResult.Success;
        }

        var registeredOptions = validationContext.GetService(typeof(Options)) as Options;
        var placeholderOptions = registeredOptions ?? (usingDefaultChecker ? new Options() : null);
        var message = ResolveMessage(result, registeredOptions);
        message = ApplyPlaceholders(message, validationContext.DisplayName, result, placeholderOptions);

        return new ValidationResult(message);
    }

    private string ResolveMessage(Result result, Options? options)
    {
        if (!string.IsNullOrWhiteSpace(ErrorMessage))
        {
            return ErrorMessage!;
        }

        var reasonSpecific = ResolveReasonSpecificMessage(result, options?.Messages);
        if (!string.IsNullOrWhiteSpace(reasonSpecific))
        {
            return reasonSpecific!;
        }

        if (!string.IsNullOrWhiteSpace(options?.ValidationMessage))
        {
            return options!.ValidationMessage!;
        }

        return GetBuiltInMessage(result);
    }

    private static string? ResolveReasonSpecificMessage(
        Result result,
        ValidationMessages? messages)
    {
        if (messages is null)
        {
            return null;
        }

        if (string.Equals(result.Category, "profanity", StringComparison.Ordinal))
        {
            return messages.Profanity;
        }

        return result.MatchKind switch
        {
            MatchKind.Exact => messages.Reserved,
            MatchKind.Compact => messages.Compact,
            MatchKind.Partial => messages.Partial,
            MatchKind.Obfuscated => messages.Obfuscated,
            MatchKind.UnicodeConfusable => messages.UnicodeConfusable,
            MatchKind.NumbersNotAllowed => messages.NumbersNotAllowed,
            MatchKind.InvalidCharacters => messages.InvalidCharacters,
            MatchKind.TooShort => messages.TooShort,
            MatchKind.TooLong => messages.TooLong,
            MatchKind.BlockedCharacter => messages.BlockedCharacter,
            MatchKind.LeadingSeparator => messages.LeadingSeparator,
            MatchKind.TrailingSeparator => messages.TrailingSeparator,
            _ => null
        };
    }

    private static string GetBuiltInMessage(Result result)
    {
        if (string.Equals(result.Category, "profanity", StringComparison.Ordinal))
        {
            return "{FieldName} contains language that is not allowed.";
        }

        return result.MatchKind switch
        {
            MatchKind.Exact => "{FieldName} is reserved and cannot be claimed.",
            MatchKind.Compact => "{FieldName} matches a reserved name after separators or punctuation are ignored.",
            MatchKind.Partial => "{FieldName} contains a reserved name and cannot be claimed.",
            MatchKind.Obfuscated => "{FieldName} appears to imitate a reserved name and cannot be claimed.",
            MatchKind.UnicodeConfusable => "{FieldName} contains Unicode lookalikes that match a reserved name.",
            MatchKind.NumbersNotAllowed => "Numbers are not allowed in {FieldName}.",
            MatchKind.InvalidCharacters => "{FieldName} contains an invalid character.",
            MatchKind.TooShort => "{FieldName} must be at least {MinimumLength} characters long.",
            MatchKind.TooLong => "{FieldName} must be no more than {MaximumLength} characters long.",
            MatchKind.BlockedCharacter => "{FieldName} contains a blocked character.",
            MatchKind.LeadingSeparator => "{FieldName} cannot start with a separator.",
            MatchKind.TrailingSeparator => "{FieldName} cannot end with a separator.",
            _ => "{FieldName} is not allowed."
        };
    }

    private static string ApplyPlaceholders(
        string message,
        string fieldName,
        Result result,
        Options? options)
    {
        var minimumLength = result.MatchKind == MatchKind.TooShort && result.LengthLimit.HasValue
            ? result.LengthLimit.Value.ToString()
            : options?.MinimumLength.ToString() ?? string.Empty;

        var maximumLength = result.MatchKind == MatchKind.TooLong && result.LengthLimit.HasValue
            ? result.LengthLimit.Value.ToString()
            : options?.MaximumLength.ToString() ?? string.Empty;

        return message
            .Replace("{FieldName}", fieldName, StringComparison.Ordinal)
            .Replace("{MatchedValue}", result.MatchedValue ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Category}", result.Category ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Character}", result.OffendingCharacter ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Index}", result.OffendingCharacterIndex?.ToString() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Length}", result.InputLength.ToString(), StringComparison.Ordinal)
            .Replace("{MinimumLength}", minimumLength, StringComparison.Ordinal)
            .Replace("{MaximumLength}", maximumLength, StringComparison.Ordinal);
    }
}
