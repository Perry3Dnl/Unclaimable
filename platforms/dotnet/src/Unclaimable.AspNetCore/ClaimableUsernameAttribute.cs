using System.ComponentModel.DataAnnotations;

namespace Unclaimable.AspNetCore;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ClaimableUsernameAttribute : ValidationAttribute
{
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

        var checker = validationContext.GetService(typeof(IChecker)) as IChecker
                      ?? Checker.Default;

        var result = checker.Check(text);
        if (!result.IsReserved)
        {
            return ValidationResult.Success;
        }

        var options = validationContext.GetService(typeof(Options)) as Options;
        var message = ResolveMessage(result, options);
        message = ApplyPlaceholders(message, validationContext.DisplayName, result, options);

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
