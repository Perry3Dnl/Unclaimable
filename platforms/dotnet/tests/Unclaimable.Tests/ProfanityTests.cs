using Xunit;

namespace Unclaimable.Tests;

public sealed class ProfanityTests
{
    [Fact]
    public void ProfanityIsEnabledByDefault()
    {
        var result = UnclaimableChecker.Default.Check("fuck");

        Assert.True(result.IsReserved);
        Assert.Equal("fuck", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(UnclaimableMatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void ProfanityCanBeDisabledExplicitly()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard,
            DisabledRules = UnclaimableRule.Profanity
        });

        Assert.True(checker.IsClaimable("fuck"));
    }

    [Fact]
    public void ProfanityUsesCompactAndObfuscationMatchingWhenStructuralRulesAreRelaxed()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            DisabledRules = UnclaimableRule.Numbers
                            | UnclaimableRule.BlockedCharacters
                            | UnclaimableRule.Whitespace
        });

        var compact = checker.Check("f-u-c-k");
        var obfuscated = checker.Check("sh1t");

        Assert.True(compact.IsReserved);
        Assert.Equal("fuck", compact.MatchedValue);
        Assert.Equal("profanity", compact.Category);
        Assert.Equal(UnclaimableMatchKind.Compact, compact.MatchKind);

        Assert.True(obfuscated.IsReserved);
        Assert.Equal("shit", obfuscated.MatchedValue);
        Assert.Equal("profanity", obfuscated.Category);
        Assert.Equal(UnclaimableMatchKind.Obfuscated, obfuscated.MatchKind);
    }

    [Fact]
    public void GenericPartialMatchingDoesNotAutomaticallyApplyToProfanity()
    {
        var checker = new UnclaimableChecker();

        Assert.True(checker.IsClaimable("cocktail"));
    }

    [Fact]
    public void ProfanityPartialMatchingCanBeEnabledExplicitly()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            ProfanityPartialMatching = true
        });

        var result = checker.Check("cocktail");

        Assert.True(result.IsReserved);
        Assert.Equal("cock", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void DetailedResultPreservesProfanityCategory()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            DisabledRules = UnclaimableRule.Numbers
        });

        var result = checker.CheckDetailed("sh1t", includeMessages: true);
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.True(result.IsReserved);
        Assert.Equal("profanity", diagnostic.Category);
        Assert.Equal("shit", diagnostic.MatchedValue);
        Assert.Equal(UnclaimableMatchKind.Obfuscated, diagnostic.Kind);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
    }
}
