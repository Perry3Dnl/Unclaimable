using FluentValidation;

namespace Unclaimable.FluentValidation;

/// <summary>FluentValidation rules backed by Unclaimable's checker.</summary>
public static class ClaimableRuleExtensions
{
    /// <summary>
    /// Requires a claimable identifier using the shared default checker.
    /// Null is accepted; add NotEmpty or NotNull when the property is required.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeClaimable<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        MustBeClaimable(ruleBuilder, Checker.Default);

    /// <summary>
    /// Requires a claimable identifier using a configured checker.
    /// The checker is captured when the validator is constructed.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeClaimable<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        IChecker checker)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);
        ArgumentNullException.ThrowIfNull(checker);

        return ruleBuilder
            .Must((_, value, context) =>
            {
                var result = checker.Check(value);
                if (result.IsClaimable)
                {
                    return true;
                }

                context.MessageFormatter.AppendArgument("MatchKind", result.MatchKind);
                context.MessageFormatter.AppendArgument("Category", result.Category ?? string.Empty);
                return false;
            })
            .WithMessage("'{PropertyName}' is not a claimable identifier ({MatchKind}).");
    }

    /// <summary>Alias for MustBeClaimable using the shared default checker.</summary>
    public static IRuleBuilderOptions<T, string?> MustNotBeReserved<T>(
        this IRuleBuilder<T, string?> ruleBuilder) =>
        MustBeClaimable(ruleBuilder);

    /// <summary>Alias for MustBeClaimable using a configured checker.</summary>
    public static IRuleBuilderOptions<T, string?> MustNotBeReserved<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        IChecker checker) =>
        MustBeClaimable(ruleBuilder, checker);
}
