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

        var checker = validationContext.GetService(typeof(IUnclaimableChecker)) as IUnclaimableChecker
                      ?? UnclaimableChecker.Default;

        return checker.IsReserved(text)
            ? new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is reserved and cannot be claimed.")
            : ValidationResult.Success;
    }
}
