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
        Assert.True(UnclaimableChecker.Default.IsReserved("facturatiehulp"));
        Assert.True(UnclaimableChecker.Default.IsClaimable("customersupport"));
        Assert.True(UnclaimableChecker.Default.IsClaimable("abrechnungshilfe"));
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
        Assert.True(checker.IsClaimable("facturatiehulp"));
        Assert.True(checker.IsClaimable("godverdomme"));
        Assert.True(checker.IsClaimable("abrechnungshilfe"));
    }

    [Fact]
    public void DutchLocalizedDatasetsAreUsedWhenDutchIsSelected()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.Dutch
        });

        Assert.Equal("support", checker.Check("facturatiehulp").Category);
        Assert.Equal("roles", checker.Check("systeembeheerder").Category);
        Assert.Equal("system", checker.Check("wachtwoordvergeten").Category);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
    }

    [Fact]
    public void GermanCanBeSelectedExplicitly()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.German
        });

        Assert.Equal("support", checker.Check("abrechnungshilfe").Category);
        Assert.Equal("roles", checker.Check("systemverwalter").Category);
        Assert.Equal("system", checker.Check("passwortvergessen").Category);
        Assert.Equal("profanity", checker.Check("scheiße").Category);
        Assert.True(checker.IsReserved("scheisse"));
        Assert.True(checker.IsClaimable("facturatiehulp"));
        Assert.True(checker.IsClaimable("customersupport"));
    }

    [Fact]
    public void MultiLanguageIncludesAllSupportedLanguages()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.Dutch,
            AllowMultiLanguage = true
        });

        Assert.True(checker.IsReserved("facturatiehulp"));
        Assert.True(checker.IsReserved("customersupport"));
        Assert.True(checker.IsReserved("abrechnungshilfe"));
        Assert.True(checker.IsReserved("godverdomme"));
        Assert.True(checker.IsReserved("fuckwaffle"));
        Assert.True(checker.IsReserved("scheiße"));
    }

    [Theory]
    [InlineData(UnclaimableLanguage.Dutch)]
    [InlineData(UnclaimableLanguage.English)]
    [InlineData(UnclaimableLanguage.German)]
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
    public void MixedLanguageValuesStillMatchEnabledProtectedSubstrings()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Language = UnclaimableLanguage.English
        });

        var result = checker.Check("klantenservice");

        Assert.True(result.IsReserved);
        Assert.Equal("service", result.MatchedValue);
        Assert.Equal("system", result.Category);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
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
