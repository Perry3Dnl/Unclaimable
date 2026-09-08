using Xunit;

namespace Unclaimable.Tests;

public sealed class ProfanityTests
{
    private static readonly Checker EnglishChecker = new Checker(new Options());

    [Fact]
    public void ProfanityIsEnabledByDefault()
    {
        var result = EnglishChecker.Check("fuck");

        Assert.True(result.IsReserved);
        Assert.Equal("fuck", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void ProfanityCanBeDisabledExplicitly()
    {
        var checker = new Checker(new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = Rule.Profanity
        });

        Assert.True(checker.IsClaimable("fuck"));
    }

    [Fact]
    public void ProfanityUsesCompactAndObfuscationMatchingWhenStructuralRulesAreRelaxed()
    {
        var checker = new Checker(new Options
        {
            DisabledRules = Rule.Numbers
                            | Rule.BlockedCharacters
                            | Rule.Whitespace
        });

        var compact = checker.Check("f-u-c-k");
        var obfuscated = checker.Check("sh1t");

        Assert.True(compact.IsReserved);
        Assert.Equal("fuck", compact.MatchedValue);
        Assert.Equal("profanity", compact.Category);
        Assert.Equal(MatchKind.Compact, compact.MatchKind);

        Assert.True(obfuscated.IsReserved);
        Assert.Equal("shit", obfuscated.MatchedValue);
        Assert.Equal("profanity", obfuscated.Category);
        Assert.Equal(MatchKind.Obfuscated, obfuscated.MatchKind);
    }

    [Fact]
    public void GenericPartialMatchingDoesNotAutomaticallyApplyToProfanity()
    {
        Assert.True(EnglishChecker.IsClaimable("cocktail"));
    }

    [Fact]
    public void ProfanityPartialMatchingCanBeEnabledExplicitly()
    {
        var checker = new Checker(new Options
        {
            ProfanityPartialMatching = true
        });

        var result = checker.Check("cocktail");

        Assert.True(result.IsReserved);
        Assert.Equal("cock", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void DetailedResultPreservesProfanityCategory()
    {
        var checker = new Checker(new Options
        {
            DisabledRules = Rule.Numbers
        });

        var result = checker.CheckDetailed("sh1t", includeMessages: true);
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.True(result.IsReserved);
        Assert.Equal("profanity", diagnostic.Category);
        Assert.Equal("shit", diagnostic.MatchedValue);
        Assert.Equal(MatchKind.Obfuscated, diagnostic.Kind);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
    }
}
