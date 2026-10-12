using Xunit;

namespace Unclaimable.Tests;

public sealed class SuffixProfanity084Tests
{
    [Fact]
    public void PrefixedExplicitRootIsRejectedWithOriginalSpan()
    {
        var checker = new Checker();
        Assert.Equal("profanity", checker.Check("dick").Category);

        var result = checker.Check("saddick");
        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("profanity", result.Category);
        Assert.Equal("dick", result.MatchedValue);
        Assert.Equal(3, result.OriginalMatchStartIndex);
        Assert.Equal(4, result.OriginalMatchLength);
    }

    [Theory]
    [InlineData("bigdick")]
    [InlineData("sad_dick")]
    public void SuffixRuleCatchesOtherPrefixedForms(string value)
    {
        var options = new Options().AllowCharacters("_");
        Assert.Equal("profanity", new Checker(options).Check(value).Category);
    }

    [Theory]
    [InlineData("dickinson")]
    [InlineData("mydickname")]
    public void SuffixRuleDoesNotMakeRootAnArbitrarySubstring(string value)
    {
        Assert.True(new Checker().IsClaimable(value));
    }

    [Fact]
    public void ExistingPartialAndProfanityControlsRemainAuthoritative()
    {
        var noPartial = new Options().DisableRule(Rule.PartialMatching);
        Assert.True(new Checker(noPartial).IsClaimable("saddick"));

        var noProfanity = new Options().DisableRule(Rule.Profanity);
        Assert.True(new Checker(noProfanity).IsClaimable("saddick"));

        var broadProfanity = new Options { ProfanityPartialMatching = true };
        Assert.Equal("profanity", new Checker(broadProfanity).Check("mydickname").Category);
    }
}
