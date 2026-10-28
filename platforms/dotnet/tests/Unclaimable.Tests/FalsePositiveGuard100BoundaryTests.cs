using Xunit;

namespace Unclaimable.Tests;

/// <summary>
/// Boundary and matching-path regression coverage for the 1.0.0 Core audit.
/// These deliberately use a synthetic slang term to avoid dataset drift.
/// </summary>
public sealed class FalsePositiveGuard100BoundaryTests
{
    private static Checker Create(FalsePositiveSensitivity sensitivity, string term = "zorb")
    {
        var options = new Options { FalsePositiveSensitivity = sensitivity };
        options.Reserve(term, "slangprofanity");
        return new Checker(options);
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void OneSidedGuardRequiresFourContextCharacters(FalsePositiveSensitivity sensitivity)
    {
        var checker = Create(sensitivity);

        Assert.True(checker.IsReserved("abczorb"));
        Assert.True(checker.IsReserved("zorbabc"));
        Assert.True(checker.IsClaimable("abcdzorb"));
        Assert.True(checker.IsClaimable("zorbabcd"));
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Low, true)]
    [InlineData(FalsePositiveSensitivity.Medium, false)]
    [InlineData(FalsePositiveSensitivity.High, false)]
    public void OneSidedRelaxationIsOptInAndSymmetric(
        FalsePositiveSensitivity sensitivity, bool shouldBlock)
    {
        var checker = Create(sensitivity);
        Assert.Equal(shouldBlock, checker.IsReserved("abcdzorb"));
        Assert.Equal(shouldBlock, checker.IsReserved("zorbabcd"));
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium, true)]
    [InlineData(FalsePositiveSensitivity.High, false)]
    public void FiveCharacterSlangTermRespectsSensitivityThreshold(
        FalsePositiveSensitivity sensitivity, bool shouldBlock)
    {
        var checker = Create(sensitivity, "zorba");
        Assert.Equal(shouldBlock, checker.IsReserved("abcdzorba"));
        Assert.Equal(shouldBlock, checker.IsReserved("zorbaabcd"));
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void SurroundedTermRemainsBlockedAcrossResultApis(FalsePositiveSensitivity sensitivity)
    {
        var checker = Create(sensitivity);
        const string value = "abcdzorbwxyz";

        Assert.True(checker.IsReserved(value));
        Assert.False(checker.IsClaimable(value));
        Assert.True(checker.Check(value).IsReserved);
        Assert.True(checker.CheckDetailed(value).IsReserved);
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void CoreProfanityIsNeverRelaxedBySlangGuard(FalsePositiveSensitivity sensitivity)
    {
        var checker = new Checker(new Options { FalsePositiveSensitivity = sensitivity });

        Assert.True(checker.IsReserved("saddick"));
        Assert.True(checker.IsReserved("dickinson"));
        Assert.True(checker.IsReserved("dickgarden"));
    }
    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium, true)]
    [InlineData(FalsePositiveSensitivity.High, false)]
    public void SixCharacterTermUsesHighOnly(
        FalsePositiveSensitivity sensitivity, bool shouldBlock)
    {
        var checker = Create(sensitivity, "zorbix");
        Assert.Equal(shouldBlock, checker.IsReserved("abcdzorbix"));
        Assert.Equal(shouldBlock, checker.IsReserved("zorbixabcd"));
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void SevenCharacterTermIsNotSuppressed(FalsePositiveSensitivity sensitivity)
    {
        var checker = Create(sensitivity, "zorbina");
        Assert.True(checker.IsReserved("abcdzorbina"));
        Assert.True(checker.IsReserved("zorbinaabcd"));
    }

    [Theory]
    [InlineData(FalsePositiveSensitivity.Medium)]
    [InlineData(FalsePositiveSensitivity.High)]
    public void IndependentCustomMatchSurvivesSuppressedSlang(FalsePositiveSensitivity sensitivity)
    {
        var options = new Options { FalsePositiveSensitivity = sensitivity };
        options.Reserve("zorb", "slangprofanity");
        options.Reserve("flarn", "custom");
        var checker = new Checker(options);

        // The first four-character slang fragment is suppressible, but the
        // independent later reservation must still reject this identifier.
        const string value = "zorbabcdflarn";
        var result = checker.Check(value);
        Assert.True(result.IsReserved);
        Assert.False(checker.IsClaimable(value));
        Assert.Equal("custom", result.Category);
    }

}
