using System.ComponentModel.DataAnnotations;

namespace Unclaimable.AspNetCore;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ClaimableUsernameAttribute : ValidationAttribute
{
    private const string DefaultValidationMessage = "{FieldName} is reserved and cannot be claimed.";

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

        if (!checker.IsReserved(text))
        {
            return ValidationResult.Success;
        }

        var options = validationContext.GetService(typeof(UnclaimableOptions)) as UnclaimableOptions;
        var message = !string.IsNullOrWhiteSpace(ErrorMessage)
            ? ErrorMessage!
            : !string.IsNullOrWhiteSpace(options?.ValidationMessage)
                ? options!.ValidationMessage!
                : DefaultValidationMessage;

        message = message.Replace("{FieldName}", validationContext.DisplayName, StringComparison.Ordinal);

        return new ValidationResult(message);
    }
}
