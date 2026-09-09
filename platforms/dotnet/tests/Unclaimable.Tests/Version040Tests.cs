using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Unclaimable.AspNetCore;
using Xunit;

namespace Unclaimable.Tests;

public sealed class Version040Tests
{
    [Fact]
    public void NewStrictnessOptionsAreDisabledByDefault()
    {
        var options = new Options();

        Assert.False(options.RejectInvisibleOnlyIdentifiers);
        Assert.False(options.RejectControlCharacters);
        Assert.False(options.RejectFormatCharacters);
        Assert.False(options.ConsistentCompactMatching);
    }

    [Theory]
    [InlineData("\uD800abc", 0)]
    [InlineData("ab\uDC00c", 2)]
    [InlineData("ab\uD800c", 2)]
    public void MalformedUtf16IsRejectedWithoutThrowing(string value, int expectedIndex)
    {
        var checker = new Checker(new Options(), new ThrowingPolicy());

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.False(result.IsClaimable);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
        Assert.Equal(expectedIndex, result.OffendingCharacterIndex);
        Assert.False(checker.IsClaimable(value));
    }

    [Fact]
    public void DetailedMalformedUtf16SkipsReservedNameNormalization()
    {
        var value = "admin\uD800";
        var checker = new Checker(new Options(), new ThrowingPolicy());

        var result = checker.CheckDetailed(value, includeMessages: true);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(MatchKind.InvalidCharacters, diagnostic.Kind);
        Assert.Equal(5, diagnostic.OffendingCharacterIndex);
        Assert.Null(diagnostic.MatchedValue);
        Assert.Contains("UTF-16", diagnostic.Message);
    }

    [Fact]
    public void ValidSurrogatePairRemainsValidUnicodeInput()
    {
        var checker = new Checker(new Options());

        var exception = Record.Exception(() => checker.Check("ab😀cd"));

        Assert.Null(exception);
        Assert.NotEqual(MatchKind.InvalidCharacters, checker.Check("ab😀cd").MatchKind);
    }

    [Fact]
    public void FormatCharacterRejectionIsOptIn()
    {
        const string value = "ab\u200Dcd";

        Assert.True(new Checker(new Options()).IsClaimable(value));

        var result = new Checker(new Options { RejectFormatCharacters = true }).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
        Assert.Equal(2, result.OffendingCharacterIndex);
        Assert.Equal("\u200D", result.OffendingCharacter);
    }

    [Fact]
    public void ControlCharacterRejectionIsOptIn()
    {
        const string value = "ab\u0001cd";

        Assert.True(new Checker(new Options()).IsClaimable(value));

        var result = new Checker(new Options { RejectControlCharacters = true }).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
        Assert.Equal(2, result.OffendingCharacterIndex);
    }

    [Theory]
    [InlineData("\u0301\u0301\u0301")]
    [InlineData("\u200D\u200C\u2060")]
    public void InvisibleOnlyRejectionIsOptIn(string value)
    {
        Assert.True(new Checker(new Options()).IsClaimable(value));

        var result = new Checker(new Options { RejectInvisibleOnlyIdentifiers = true }).Check(value);
        Assert.Equal(MatchKind.InvalidCharacters, result.MatchKind);
    }

    [Fact]
    public void InvisibleOnlyApproximationAllowsVisibleContentWithCombiningMarks()
    {
        var checker = new Checker(new Options { RejectInvisibleOnlyIdentifiers = true });

        Assert.True(checker.IsClaimable("a\u0301bc"));
    }

    [Fact]
    public void UnicodeStrictnessOptionsAreCapturedAtConstruction()
    {
        var options = new Options();
        var checker = new Checker(options);
        options.RejectFormatCharacters = true;

        Assert.True(checker.IsClaimable("ab\u200Dcd"));
    }

    [Fact]
    public void LegacyCompactPartialBehaviorIsPreservedByDefault()
    {
        var checker = CreateCompactInteractionChecker(consistent: false, compactEnabled: false, partialEnabled: true);

        var result = checker.Check("xxfoo-barxx");

        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("foobar", result.MatchedValue);
    }

    [Fact]
    public void ConsistentCompactMatchingSkipsCompactPartialWhenCompactRuleIsDisabled()
    {
        var checker = CreateCompactInteractionChecker(consistent: true, compactEnabled: false, partialEnabled: true);

        Assert.True(checker.IsClaimable("xxfoo-barxx"));
    }

    [Fact]
    public void ConsistentCompactMatchingKeepsExactPartialWhenCompactRuleIsDisabled()
    {
        var checker = CreateCompactInteractionChecker(consistent: true, compactEnabled: false, partialEnabled: true);

        var result = checker.Check("xxfoobarxx");

        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("foobar", result.MatchedValue);
    }

    [Fact]
    public void CompactPartialStillWorksWhenCompactRuleIsEnabled()
    {
        var checker = CreateCompactInteractionChecker(consistent: true, compactEnabled: true, partialEnabled: true);

        Assert.Equal(MatchKind.Partial, checker.Check("xxfoo-barxx").MatchKind);
    }

    [Fact]
    public void LegacyObfuscationStillCompactsWhenCompactRuleIsDisabled()
    {
        var checker = CreateCompactInteractionChecker(consistent: false, compactEnabled: false, partialEnabled: false);

        var result = checker.Check("f00-bar");

        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
        Assert.Equal("foobar", result.MatchedValue);
    }

    [Fact]
    public void ConsistentObfuscationPreservesSeparatorsWhenCompactRuleIsDisabled()
    {
        var checker = CreateCompactInteractionChecker(consistent: true, compactEnabled: false, partialEnabled: false);

        Assert.True(checker.IsClaimable("f00-bar"));
    }

    [Fact]
    public void ConsistentObfuscationUsesLegacyCompactionWhenCompactRuleIsEnabled()
    {
        var checker = CreateCompactInteractionChecker(consistent: true, compactEnabled: true, partialEnabled: false);

        Assert.Equal(MatchKind.Obfuscated, checker.Check("f00-bar").MatchKind);
    }

    [Fact]
    public void UnicodeConfusableMatchingRemainsIndependentOfCompactConsistency()
    {
        var options = RelaxStructuralRules(new Options
        {
            Strictness = Strictness.Standard,
            ConsistentCompactMatching = true,
            DisabledRules = Rule.CompactMatching
        });
        options.AdditionalReserved.Add("qaq");
        var checker = new Checker(options);

        var result = checker.Check("q\u0430q");

        Assert.Equal(MatchKind.UnicodeConfusable, result.MatchKind);
        Assert.Equal("qaq", result.MatchedValue);
    }

    [Fact]
    public void ExactMatchReportsOriginalInputSpan()
    {
        var result = Checker.Default.Check("ADMIN");

        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(0, result.OriginalMatchStartIndex);
        Assert.Equal(5, result.OriginalMatchLength);
    }

    [Fact]
    public void TrimmedExactMatchReportsSpanInsideOriginalInput()
    {
        var options = new Options { DisabledRules = Rule.Whitespace };
        var result = new Checker(options).Check(" admin ");

        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(1, result.OriginalMatchStartIndex);
        Assert.Equal(5, result.OriginalMatchLength);
    }

    [Fact]
    public void CompactOriginalSpanIncludesInterveningPunctuation()
    {
        var options = RelaxStructuralRules(new Options());
        options.AdditionalReserved.Add("foobar");
        var result = new Checker(options).Check("foo-bar");

        Assert.Equal(MatchKind.Compact, result.MatchKind);
        Assert.Equal(0, result.MatchStartIndex);
        Assert.Equal(6, result.MatchLength);
        Assert.Equal(0, result.OriginalMatchStartIndex);
        Assert.Equal(7, result.OriginalMatchLength);
    }

    [Fact]
    public void CompactPartialOriginalSpanIncludesInterveningPunctuation()
    {
        var options = RelaxStructuralRules(new Options());
        options.AdditionalReserved.Add("foobar");
        var result = new Checker(options).Check("xxfoo-barzz");

        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(2, result.MatchStartIndex);
        Assert.Equal(6, result.MatchLength);
        Assert.Equal(2, result.OriginalMatchStartIndex);
        Assert.Equal(7, result.OriginalMatchLength);
    }

    [Fact]
    public void UncertainNormalizationLeavesOriginalSpanUnavailable()
    {
        var options = new Options { DisabledRules = Rule.MinimumLength };
        options.AdditionalReserved.Add("ffi");
        var result = new Checker(options).Check("\uFB03");

        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Null(result.OriginalMatchStartIndex);
        Assert.Null(result.OriginalMatchLength);
    }

    [Fact]
    public void DetailedReservedDiagnosticCarriesOriginalSpan()
    {
        var options = RelaxStructuralRules(new Options());
        options.AdditionalReserved.Add("foobar");
        var result = new Checker(options).CheckDetailed("foo-bar");

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(0, diagnostic.OriginalMatchStartIndex);
        Assert.Equal(7, diagnostic.OriginalMatchLength);
    }

    [Fact]
    public void LengthFailuresCarryCapturedThresholds()
    {
        var checker = new Checker(new Options { MinimumLength = 5, MaximumLength = 8 });

        Assert.Equal(5, checker.Check("four").LengthLimit);
        Assert.Equal(8, checker.Check("toolonggg").LengthLimit);
    }

    [Fact]
    public void LengthLimitUsesCheckerSnapshotAfterOptionsMutation()
    {
        var options = new Options { MinimumLength = 5 };
        var checker = new Checker(options);
        options.MinimumLength = 8;

        var result = checker.Check("four");

        Assert.Equal(MatchKind.TooShort, result.MatchKind);
        Assert.Equal(5, result.LengthLimit);
    }

    [Fact]
    public void AttributeWithoutDependencyInjectionIncludesDefaultMinimumLength()
    {
        var error = Assert.Single(ValidateWithoutServices(new AttributeModel { Username = "ab" }));

        Assert.Equal("Username must be at least 3 characters long.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeWithoutDependencyInjectionIncludesDefaultMaximumLength()
    {
        var error = Assert.Single(ValidateWithoutServices(new AttributeModel { Username = new string('x', 33) }));

        Assert.Equal("Username must be no more than 32 characters long.", error.ErrorMessage);
    }

    [Fact]
    public void AttributePrefersCapturedResultLengthOverMutatedRegisteredOptions()
    {
        using var provider = new ServiceCollection()
            .AddUnclaimable(options => options.MinimumLength = 5)
            .BuildServiceProvider();

        _ = provider.GetRequiredService<IChecker>();
        provider.GetRequiredService<Options>().MinimumLength = 8;

        var error = Assert.Single(ValidateWithServices(new AttributeModel { Username = "four" }, provider));
        Assert.Equal("Username must be at least 5 characters long.", error.ErrorMessage);
    }

    [Fact]
    public void AttributeFallsBackToRegisteredOptionsForCustomCheckerResults()
    {
        var options = new Options { MinimumLength = 7 };
        using var provider = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton<IChecker>(new TooShortChecker())
            .BuildServiceProvider();

        var error = Assert.Single(ValidateWithServices(new AttributeModel { Username = "value" }, provider));
        Assert.Equal("Username must be at least 7 characters long.", error.ErrorMessage);
    }

    private static Checker CreateCompactInteractionChecker(bool consistent, bool compactEnabled, bool partialEnabled)
    {
        var disabled = Rule.Numbers | Rule.BlockedCharacters | Rule.Whitespace | Rule.LeadingSeparator | Rule.TrailingSeparator;
        if (!compactEnabled)
        {
            disabled |= Rule.CompactMatching;
        }

        if (!partialEnabled)
        {
            disabled |= Rule.PartialMatching;
        }

        var options = new Options
        {
            Strictness = Strictness.Standard,
            PartialMatching = partialEnabled,
            ConsistentCompactMatching = consistent,
            DisabledRules = disabled
        };
        options.AdditionalReserved.Add("foobar");
        return new Checker(options);
    }

    private static Options RelaxStructuralRules(Options options)
    {
        options.DisabledRules |= Rule.BlockedCharacters | Rule.Whitespace | Rule.LeadingSeparator | Rule.TrailingSeparator;
        return options;
    }

    private static IReadOnlyList<ValidationResult> ValidateWithoutServices(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Assert.False(Validator.TryValidateObject(model, context, results, validateAllProperties: true));
        return results;
    }

    private static IReadOnlyList<ValidationResult> ValidateWithServices(object model, IServiceProvider provider)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, provider, items: null);
        Assert.False(Validator.TryValidateObject(model, context, results, validateAllProperties: true));
        return results;
    }

    private sealed class AttributeModel
    {
        [Display(Name = "Username")]
        [ClaimableUsername]
        public string Username { get; init; } = string.Empty;
    }

    private sealed class ThrowingPolicy : IPolicy
    {
        public IReadOnlyCollection<string> BlockedCharacters => Array.Empty<string>();
        public void BlockCharacter(string value) => throw new InvalidOperationException("Policy should not be used.");
        public void BlockCharacters(params string[] values) => throw new InvalidOperationException("Policy should not be used.");
        public bool AllowCharacter(string value) => throw new InvalidOperationException("Policy should not be used.");
        public void AllowCharacters(params string[] values) => throw new InvalidOperationException("Policy should not be used.");
        public bool IsCharacterBlocked(string value) => throw new InvalidOperationException("Policy should not be used.");
        public bool IsCharacterExplicitlyAllowed(string value) => throw new InvalidOperationException("Policy should not be used.");
    }

    private sealed class TooShortChecker : IChecker
    {
        public bool IsReserved(string? value) => true;
        public bool IsClaimable(string? value) => false;
        public Result Check(string? value) => Result.TooShort(value);
        public DetailedResult CheckDetailed(string? value, bool includeMessages = false) =>
            new DetailedResult(value, new[] { new Diagnostic(MatchKind.TooShort) });
    }
}
