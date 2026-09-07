using Xunit;

namespace Unclaimable.Tests;

public sealed class LanguageTests
{
    [Fact]
    public void DutchIsTheDefaultLanguage()
    {
        var options = new UnclaimableOptions();

        Assert.Equal(UnclaimableLanguage.Dutch, options.Language);
        Assert.False(options.AllowMultiLanguage);
        Assert.True(UnclaimableChecker.Default.IsReserved("klantenservice"));
        Assert.True(UnclaimableChecker.Default.IsClaimable("customersupport"));
    }

    [Fact]
    public void EnglishCanBeSelectedExplicitly()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.English
        });

        Assert.True(checker.IsReserved("customersupport"));
        Assert.True(checker.IsReserved("fuckwaffle"));
        Assert.True(checker.IsClaimable("klantenservice"));
        Assert.True(checker.IsClaimable("godverdomme"));
    }

    [Fact]
    public void DutchLocalizedDatasetsAreUsedWhenDutchIsSelected()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.Dutch
        });

        Assert.Equal("support", checker.Check("klantenservice").Category);
        Assert.Equal("roles", checker.Check("systeembeheerder").Category);
        Assert.Equal("system", checker.Check("wachtwoordvergeten").Category);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
    }

    [Fact]
    public void MultiLanguageIncludesEnglishAndDutch()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.Dutch,
            AllowMultiLanguage = true
        });

        Assert.True(checker.IsReserved("klantenservice"));
        Assert.True(checker.IsReserved("customersupport"));
        Assert.True(checker.IsReserved("godverdomme"));
        Assert.True(checker.IsReserved("fuckwaffle"));
    }

    [Theory]
    [InlineData(UnclaimableLanguage.Dutch)]
    [InlineData(UnclaimableLanguage.English)]
    public void GlobalDatasetsAreAlwaysIncluded(UnclaimableLanguage language)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = language
        });

        Assert.Equal("brands", checker.Check("rituals").Category);
        Assert.Equal("technology", checker.Check("homeassistant").Category);
    }

    [Fact]
    public void InvalidLanguageValueFailsFast()
    {
        var options = new UnclaimableOptions
        {
            Language = (UnclaimableLanguage)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new UnclaimableChecker(options));
    }
}
