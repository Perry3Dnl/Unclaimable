using Xunit;

namespace Unclaimable.Tests;

public sealed class StrictnessTests
{
    [Fact]
    public void UnknownStrictnessValueIsRejected()
    {
        var options = new UnclaimableOptions
        {
            Strictness = (UnclaimableStrictness)99
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new UnclaimableChecker(options));
    }

    [Fact]
    public void BasicStrictnessUsesOnlyExactAndCompactMatching()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Basic
        };
        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("nike"));
        Assert.True(checker.IsReserved("customer-service"));
        Assert.True(checker.IsClaimable("N1k3"));
        Assert.True(checker.IsClaimable("аpple"));
        Assert.True(checker.IsClaimable("old-admin"));
        Assert.Equal(
            UnclaimableRule.Exact | UnclaimableRule.Compact,
            options.EnabledRules);
    }

    [Fact]
    public void StandardStrictnessEnablesImpersonationRulesButNotPartialMatching()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard
        };
        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("N1k3"));
        Assert.True(checker.IsReserved("аpple"));
        Assert.True(checker.IsClaimable("old-admin"));
        Assert.Equal(
            UnclaimableRule.Exact
            | UnclaimableRule.Compact
            | UnclaimableRule.Obfuscation
            | UnclaimableRule.UnicodeConfusables,
            options.EnabledRules);
    }

    [Theory]
    [InlineData("admin2")]
    [InlineData("old-admin")]
    [InlineData("admin-old")]
    [InlineData("administrator2")]
    public void StandardStrictnessKeepsPartialMatchingOptIn(string value)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard
        });

        Assert.True(checker.IsClaimable(value));
    }

    [Theory]
    [InlineData("admin2", "admin")]
    [InlineData("old-admin", "admin")]
    [InlineData("admin-old", "admin")]
    [InlineData("administrator2", "administrator")]
    public void StrictStrictnessRejectsEmbeddedReservedNames(string value, string expectedMatch)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.False(result.IsClaimable);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
    }

    [Fact]
    public void StrictStrictnessAlsoAppliesToAdditionalReservedValues()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict
        };
        options.AdditionalReserved.Add("examplebrand");

        var checker = new UnclaimableChecker(options);

        var result = checker.Check("old-examplebrand");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal("examplebrand", result.MatchedValue);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void ExplicitRuleOverrideWinsOverStrictnessPreset()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            PartialMatching = false,
            ObfuscationMatching = false
        };
        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsClaimable("old-admin"));
        Assert.True(checker.IsClaimable("N1k3"));
        Assert.False(options.EnabledRules.HasFlag(UnclaimableRule.Partial));
        Assert.False(options.EnabledRules.HasFlag(UnclaimableRule.Obfuscation));
        Assert.True(options.EnabledRules.HasFlag(UnclaimableRule.UnicodeConfusables));
    }

    [Fact]
    public void ResetMatchingRuleOverridesRestoresCurrentPreset()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            CompactMatching = false,
            PartialMatching = false,
            ObfuscationMatching = false,
            UnicodeConfusableMatching = false
        };

        options.ResetMatchingRuleOverrides();

        Assert.True(options.CompactMatching);
        Assert.True(options.PartialMatching);
        Assert.True(options.ObfuscationMatching);
        Assert.True(options.UnicodeConfusableMatching);
    }

    [Fact]
    public void EnabledRulesIncludesIndependentPolicies()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            ProfanityMatching = true,
            ProfanityPartialMatching = true,
            AllowNumbers = false,
            AsciiOnly = true
        };

        Assert.True(options.EnabledRules.HasFlag(UnclaimableRule.Profanity));
        Assert.True(options.EnabledRules.HasFlag(UnclaimableRule.ProfanityPartial));
        Assert.True(options.EnabledRules.HasFlag(UnclaimableRule.RejectNumbers));
        Assert.True(options.EnabledRules.HasFlag(UnclaimableRule.AsciiOnly));
    }

    [Fact]
    public void StrictStrictnessStillHonorsPartialMinimumLength()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict
        });

        Assert.True(checker.IsReserved("api"));
        Assert.True(checker.IsClaimable("api123"));
        Assert.True(checker.IsClaimable("rapid"));
    }

    [Fact]
    public void StrictStrictnessDoesNotImplicitlyEnableProfanityPartialMatching()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            ProfanityMatching = true
        });

        Assert.True(checker.IsReserved("cock"));
        Assert.True(checker.IsClaimable("cocktail"));
    }

    [Fact]
    public void ProfanityPartialMatchingCanStillBeEnabledExplicitlyInStrictMode()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            ProfanityMatching = true,
            ProfanityPartialMatching = true
        });

        var result = checker.Check("cocktail");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal("cock", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
    }
}
