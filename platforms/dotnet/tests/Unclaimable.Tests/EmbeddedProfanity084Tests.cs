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

    [Fact]
    public void ScopedProfanityExceptionDoesNotHideIndependentCustomPartialMatch()
    {
        const string value = "dickzorb";
        var options = new Options();
        options.AdditionalReserved.Add("zorb");
        options.AllowIdentifierForRule(value, Rule.Profanity);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("custom", result.Category);
        Assert.Equal("zorb", result.MatchedValue);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Theory]
    [InlineData("dickzorb")]
    [InlineData("zorbdick")]
    public void Regression_ScopedExceptionMustContinueScanningOtherReservedTerms(string value)
    {
        var options = new Options();
        options.AdditionalReserved.Add("zorb");
        options.AllowIdentifierForRule(value, Rule.Profanity);

        var checker = new Checker(options);
        var result = checker.Check(value);
        var detailed = checker.CheckDetailed(value);

        Assert.True(result.IsReserved);
        Assert.False(checker.IsClaimable(value));
        Assert.Equal("custom", result.Category);
        Assert.Equal("zorb", result.MatchedValue);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.True(detailed.IsReserved);
    }

    [Fact]
    public void Regression_ScopedExceptionAllowsOnlyTheExemptedTerm()
    {
        const string value = "saddick";
        var options = new Options().AllowIdentifierForRule(value, Rule.Profanity);
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable(value));
        Assert.False(checker.IsReserved(value));
        Assert.True(new Checker().IsReserved(value));
    }
}
