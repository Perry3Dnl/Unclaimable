using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class HateSpeech084Tests
{
    [Theory]
    [InlineData("wetback")]
    [InlineData("fag")]
    [InlineData("fags")]
    [InlineData("faggot")]
    [InlineData("towelhead")]
    public void CoreProtectsHighConfidenceIdentitySlurs(string identifier)
    {
        var result = new Checker().Check(identifier);
        Assert.True(result.IsReserved, $"Expected {identifier} to be protected.");
        Assert.Equal("profanity", result.Category);
    }

    [Theory]
    [InlineData("saggyfag")]
    [InlineData("fagprofile")]
    [InlineData("myfagaccount")]
    public void CoreBlocksEmbeddedHighConfidenceSlurRootByDefault(string identifier)
    {
        var result = new Checker().Check(identifier);
        Assert.True(result.IsReserved, $"Expected {identifier} to be protected.");
        Assert.Equal("profanity", result.Category);
    }

    [Theory]
    [InlineData("cocksmoker")]
    [InlineData("dumbfucks")]
    [InlineData("shitholes")]
    [InlineData("whoremongers")]
    public void CoreProtectsNewStandaloneProfanityWords(string identifier)
    {
        var result = new Checker().Check(identifier);
        Assert.True(result.IsReserved, $"Expected {identifier} to be reserved by default.");
        Assert.Equal("profanity", result.Category);
    }

    [Fact]
    public void DisablingProfanityDoesNotBlockExplicitProfanityRoots()
    {
        var options = new Options { ProfanityMatching = false };
        Assert.True(new Checker(options).IsClaimable("saggyfag"));
    }

    [Fact]
    public void ExtendedCategoryIsOptInAndUsesCoreChecker()
    {
        const string identifier = "dykehaters";
        Assert.True(new Checker().IsClaimable(identifier));

        var options = new Options();
        options.UseExtendedData();
        var result = new Checker(options).Check(identifier);

        Assert.True(result.IsReserved);
        Assert.Equal("hatespeech", result.Category);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.HateSpeech) >= 60);
    }

    [Fact]
    public void ExtendedHateSpeechCanBeDisabledWithoutDisablingCoreProtection()
    {
        var options = new Options();
        options.UseExtendedData(extended => extended.DisableCategory(ExtendedCategory.HateSpeech));
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("dykehaters"));
        Assert.True(checker.IsReserved("wetback"));
    }

    [Fact]
    public void ExtendedHateSpeechExceptionsAreExact()
    {
        var options = new Options();
        options.UseExtendedData(extended => extended.AllowedIdentifiers.Add("dykehaters"));
        var checker = new Checker(options);

        Assert.NotEqual("hatespeech", checker.Check("dykehaters").Category);
        Assert.True(checker.IsReserved("trannyhaters"));
    }

    [Fact]
    public void ExtendedDoesNotTurnWholeIdentifierIntoSubstringMatching()
    {
        var options = new Options();
        options.UseExtendedData();
        var checker = new Checker(options);

        Assert.True(checker.IsReserved("dykehaters"));
        Assert.True(checker.IsReserved("mydykehatersarchive"));
    }
}
