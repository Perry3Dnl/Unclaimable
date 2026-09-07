using Xunit;

namespace Unclaimable.Tests;

public sealed class ProfanityTests
{
    [Fact]
    public void ProfanityIsDisabledByDefault()
    {
        var checker = new UnclaimableChecker();

        Assert.True(checker.IsClaimable("fuck"));
        Assert.True(checker.IsClaimable("sh1t"));
    }

    [Fact]
    public void ProfanityCanBeEnabledExplicitly()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            ProfanityMatching = true
        });

        var result = checker.Check("fuck");

        Assert.True(result.IsReserved);
        Assert.Equal("fuck", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(UnclaimableMatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void ProfanityUsesCompactAndObfuscationMatching()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            ProfanityMatching = true
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
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            PartialMatching = true,
            ProfanityMatching = true
        });

        Assert.True(checker.IsClaimable("cocktail"));
    }

    [Fact]
    public void ProfanityPartialMatchingRequiresItsOwnExplicitOptIn()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            PartialMatching = true,
            ProfanityMatching = true,
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
            ProfanityMatching = true
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
