using System.ComponentModel.DataAnnotations;

namespace Unclaimable.AspNetCore;

/// <summary>
/// Validates that a string identifier can be claimed under the registered Unclaimable policy.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ClaimableUsernameAttribute : ValidationAttribute
{
    /// <inheritdoc />
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

        var checker = validationContext.GetService(typeof(IUnclaimableChecker)) as IUnclaimableChecker
                      ?? UnclaimableChecker.Default;

        var result = checker.Check(text);
        if (!result.IsReserved)
        {
            return ValidationResult.Success;
        }

        var options = validationContext.GetService(typeof(UnclaimableOptions)) as UnclaimableOptions;
        var message = ResolveMessage(result, options);
        message = ApplyPlaceholders(message, validationContext.DisplayName, result, options);

        return new ValidationResult(message);
    }

    private string ResolveMessage(UnclaimableResult result, UnclaimableOptions? options)
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
        UnclaimableResult result,
        UnclaimableValidationMessages? messages)
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
            UnclaimableMatchKind.Exact => messages.Reserved,
            UnclaimableMatchKind.Compact => messages.Compact,
            UnclaimableMatchKind.Partial => messages.Partial,
            UnclaimableMatchKind.Obfuscated => messages.Obfuscated,
            UnclaimableMatchKind.UnicodeConfusable => messages.UnicodeConfusable,
            UnclaimableMatchKind.NumbersNotAllowed => messages.NumbersNotAllowed,
            UnclaimableMatchKind.InvalidCharacters => messages.InvalidCharacters,
            UnclaimableMatchKind.TooShort => messages.TooShort,
            UnclaimableMatchKind.TooLong => messages.TooLong,
            UnclaimableMatchKind.BlockedCharacter => messages.BlockedCharacter,
            UnclaimableMatchKind.LeadingSeparator => messages.LeadingSeparator,
            UnclaimableMatchKind.TrailingSeparator => messages.TrailingSeparator,
            _ => null
        };
    }

    private static string GetBuiltInMessage(UnclaimableResult result)
    {
        if (string.Equals(result.Category, "profanity", StringComparison.Ordinal))
        {
            return "{FieldName} contains language that is not allowed.";
        }

        return result.MatchKind switch
        {
            UnclaimableMatchKind.Exact => "{FieldName} is reserved and cannot be claimed.",
            UnclaimableMatchKind.Compact => "{FieldName} matches a reserved name after separators or punctuation are ignored.",
            UnclaimableMatchKind.Partial => "{FieldName} contains a reserved name and cannot be claimed.",
            UnclaimableMatchKind.Obfuscated => "{FieldName} appears to imitate a reserved name and cannot be claimed.",
            UnclaimableMatchKind.UnicodeConfusable => "{FieldName} contains Unicode lookalikes that match a reserved name.",
            UnclaimableMatchKind.NumbersNotAllowed => "Numbers are not allowed in {FieldName}.",
            UnclaimableMatchKind.InvalidCharacters => "{FieldName} contains an invalid character.",
            UnclaimableMatchKind.TooShort => "{FieldName} must be at least {MinimumLength} characters long.",
            UnclaimableMatchKind.TooLong => "{FieldName} must be no more than {MaximumLength} characters long.",
            UnclaimableMatchKind.BlockedCharacter => "{FieldName} contains a blocked character.",
            UnclaimableMatchKind.LeadingSeparator => "{FieldName} cannot start with a separator.",
            UnclaimableMatchKind.TrailingSeparator => "{FieldName} cannot end with a separator.",
            _ => "{FieldName} is not allowed."
        };
    }

    private static string ApplyPlaceholders(
        string message,
        string fieldName,
        UnclaimableResult result,
        UnclaimableOptions? options)
    {
        return message
            .Replace("{FieldName}", fieldName, StringComparison.Ordinal)
            .Replace("{MatchedValue}", result.MatchedValue ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Category}", result.Category ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Character}", result.OffendingCharacter ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Index}", result.OffendingCharacterIndex?.ToString() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{Length}", result.InputLength.ToString(), StringComparison.Ordinal)
            .Replace("{MinimumLength}", options?.MinimumLength.ToString() ?? string.Empty, StringComparison.Ordinal)
            .Replace("{MaximumLength}", options?.MaximumLength.ToString() ?? string.Empty, StringComparison.Ordinal);
    }
}
