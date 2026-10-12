using Xunit;

namespace Unclaimable.Tests;

public sealed class DutchProfanity085Tests
{
    [Fact]
    public void DutchExplicitCompoundIsBlockedByDefault()
    {
        var result = new Checker(new Options()).Check("dikkepik");

        Assert.True(result.IsReserved);
        Assert.Equal("profanity", result.Category);
        Assert.Equal("dikkepik", result.MatchedValue);
    }

    [Fact]
    public void DutchExplicitCompoundIsBlockedWhenDutchIsSelected()
    {
        var options = new Options { MultilingualProfanityMatching = false };
        options.AddLanguage(Language.Dutch);

        Assert.Equal("profanity", new Checker(options).Check("dikkepik").Category);
    }

    [Fact]
    public void DutchExplicitCompoundRespectsLanguageScope()
    {
        var options = new Options { MultilingualProfanityMatching = false };

        Assert.True(new Checker(options).IsClaimable("dikkepik"));
    }

    [Fact]
    public void DutchExactEntryDoesNotBlockUnrelatedSimilarWords()
    {
        Assert.True(new Checker(new Options()).IsClaimable("pikachu"));
    }
}
