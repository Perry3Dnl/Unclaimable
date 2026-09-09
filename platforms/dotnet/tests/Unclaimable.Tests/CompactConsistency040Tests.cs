using Xunit;

namespace Unclaimable.Tests;

public sealed class CompactConsistency040Tests
{
    [Fact]
    public void LegacyCompactPartialBehaviorIsPreservedByDefault()
    {
        var checker = CreateChecker(consistent: false, compactEnabled: false, partialEnabled: true);

        var result = checker.Check("xxfoo-barxx");

        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("foobar", result.MatchedValue);
    }

    [Fact]
    public void ConsistentCompactMatchingSkipsCompactPartialWhenCompactRuleIsDisabled()
    {
        var checker = CreateChecker(consistent: true, compactEnabled: false, partialEnabled: true);

        Assert.True(checker.IsClaimable("xxfoo-barxx"));
    }

    [Fact]
    public void ConsistentCompactMatchingKeepsExactPartialWhenCompactRuleIsDisabled()
    {
        var checker = CreateChecker(consistent: true, compactEnabled: false, partialEnabled: true);

        var result = checker.Check("xxfoobarxx");

        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("foobar", result.MatchedValue);
    }

    [Fact]
    public void CompactPartialStillWorksWhenCompactRuleIsEnabled()
    {
        var checker = CreateChecker(consistent: true, compactEnabled: true, partialEnabled: true);

        Assert.Equal(MatchKind.Partial, checker.Check("xxfoo-barxx").MatchKind);
    }

    [Fact]
    public void LegacyObfuscationStillCompactsWhenCompactRuleIsDisabled()
    {
        var checker = CreateChecker(consistent: false, compactEnabled: false, partialEnabled: false);

        var result = checker.Check("f00-bar");

        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
        Assert.Equal("foobar", result.MatchedValue);
    }

    [Fact]
    public void ConsistentObfuscationPreservesSeparatorsWhenCompactRuleIsDisabled()
    {
        var checker = CreateChecker(consistent: true, compactEnabled: false, partialEnabled: false);

        Assert.True(checker.IsClaimable("f00-bar"));
    }

    [Fact]
    public void ConsistentObfuscationUsesLegacyCompactionWhenCompactRuleIsEnabled()
    {
        var checker = CreateChecker(consistent: true, compactEnabled: true, partialEnabled: false);

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

    private static Checker CreateChecker(bool consistent, bool compactEnabled, bool partialEnabled)
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
}
