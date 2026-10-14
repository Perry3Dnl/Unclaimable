using Xunit;

namespace Unclaimable.Tests;

public sealed class FalsePositiveGuard085Tests
{
    private static Checker Create(FalsePositiveSensitivity sensitivity, string term = "zorb")
    {
        var options = new Options { FalsePositiveSensitivity = sensitivity }.AllowCharacters("_");
        options.Reserve(term, "slangprofanity");
        return new Checker(options);
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Low)]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void ExactAndMiddleMatchesRemainBlocked(FalsePositiveSensitivity sensitivity)
    {
        var checker = Create(sensitivity);

        Assert.True(checker.IsReserved("zorb"));
        Assert.Equal("slangprofanity", checker.Check("leftzorbright").Category);
        Assert.False(checker.IsClaimable("leftzorbright"));
        Assert.True(checker.CheckDetailed("leftzorbright").IsReserved);
    }

    [Fact]
    public void LowIsTheDefaultAndPreservesOneSidedMatches()
    {
        var options = new Options();
        options.Reserve("zorb", "slangprofanity");
        var checker = new Checker(options);

        Assert.Equal(FalsePositiveSensitivity.Low, options.FalsePositiveSensitivity);
        Assert.True(checker.IsReserved("leftzorb"));
        Assert.True(checker.IsReserved("zorbright"));
    }

    [Theory]
    [InlineData("leftzorb")]
    [InlineData("zorbright")]
    public void MediumAllowsShortOneSidedSlang(string value)
    {
        var checker = Create(FalsePositiveSensitivity.Medium);

        Assert.True(checker.IsClaimable(value));
        Assert.False(checker.IsReserved(value));
        Assert.False(checker.Check(value).IsReserved);
        Assert.False(checker.CheckDetailed(value).IsReserved);
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void SingleCharacterAppendagesRemainBlocked(FalsePositiveSensitivity sensitivity)
    {
        var checker = Create(sensitivity);
        Assert.True(checker.IsReserved("zorbx"));
        Assert.True(checker.IsReserved("xzorb"));
        Assert.True(checker.IsReserved("zorb_"));
        Assert.True(checker.IsReserved("z0rbx"));
    }

    [Fact]
    public void HighAllowsLongerOneSidedSlang()
    {
        var medium = Create(FalsePositiveSensitivity.Medium, "zorbix");
        var high = Create(FalsePositiveSensitivity.High, "zorbix");

        Assert.True(medium.IsReserved("leftzorbix"));
        Assert.True(high.IsClaimable("leftzorbix"));
        Assert.True(high.IsClaimable("zorbixright"));
        Assert.True(high.IsReserved("leftzorbixright"));
        Assert.True(high.IsReserved("zorbix"));
    }

    [Fact]
    public void ASecondMiddleOccurrenceIsStillDetected()
    {
        var checker = Create(FalsePositiveSensitivity.High);

        Assert.Equal("slangprofanity", checker.Check("zorbmiddlezorbright").Category);
        Assert.False(checker.IsClaimable("zorbmiddlezorbright"));
    }

    [Fact]
    public void CompactAndObfuscatedOneSidedSlangUseTheSameGuard()
    {
        var checker = Create(FalsePositiveSensitivity.Medium);
        Assert.True(checker.IsClaimable("left_zorb"));
        Assert.True(checker.IsClaimable("leftz0rb"));
        Assert.True(checker.IsReserved("left_zorb_right"));
        Assert.True(checker.IsReserved("leftz0rbright"));
    }

    [Fact]
    public void OtherCategoriesAndCoreProfanityRemainBlocked()
    {
        var options = new Options { FalsePositiveSensitivity = FalsePositiveSensitivity.High };
        options.Reserve("zorb", "hatespeech");
        var checker = new Checker(options);

        Assert.True(checker.IsReserved("leftzorb"));
        Assert.True(checker.IsReserved("saddick"));
        Assert.True(checker.IsReserved("dickinson"));
    }

    [Fact]
    public void SuppressedSlangDoesNotHideAnotherReservation()
    {
        var options = new Options { FalsePositiveSensitivity = FalsePositiveSensitivity.High };
        options.Reserve("zorb", "slangprofanity");
        options.Reserve("flarn", "custom");
        var checker = new Checker(options);

        Assert.Equal("custom", checker.Check("zorbflarn").Category);
        Assert.False(checker.IsClaimable("zorbflarn"));
    }

    [Fact]
    public void InvalidSensitivityIsRejectedAtConstruction()
    {
        var options = new Options { FalsePositiveSensitivity = (FalsePositiveSensitivity)99 };
        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(options));
    }
}
