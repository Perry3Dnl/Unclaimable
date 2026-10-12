using Xunit;

namespace Unclaimable.Tests;

public sealed class EmbeddedProfanity084Tests
{
    [Fact]
    public void ExplicitRootIsRejectedInsideIdentifierWithOriginalSpan()
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
    [InlineData("dickinson")]
    [InlineData("dickens")]
    [InlineData("mydickname")]
    public void ExplicitRootIsRejectedAtAnyPosition(string value)
    {
        var options = new Options().AllowCharacters("_");
        var result = new Checker(options).Check(value);
        Assert.True(result.IsReserved);
        Assert.Equal("profanity", result.Category);
        Assert.Equal("dick", result.MatchedValue);
    }

    [Fact]
    public void ExistingPartialAndProfanityControlsRemainAuthoritative()
    {
        var noPartial = new Options().DisableRule(Rule.PartialMatching);
        Assert.True(new Checker(noPartial).IsClaimable("saddick"));
        Assert.True(new Checker(noPartial).IsClaimable("dickinson"));

        var noProfanity = new Options().DisableRule(Rule.Profanity);
        Assert.True(new Checker(noProfanity).IsClaimable("saddick"));
        Assert.True(new Checker(noProfanity).IsClaimable("dickinson"));
    }
}
