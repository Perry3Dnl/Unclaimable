using Xunit;

namespace Unclaimable.Tests;

public sealed class LanguageTests
{
    [Fact]
    public void EnglishIsEnabledByDefault()
    {
        var options = new UnclaimableOptions();

        Assert.Contains(UnclaimableLanguage.English, options.Languages);
        Assert.Single(options.Languages);
        Assert.True(UnclaimableChecker.Default.IsReserved("customersupport"));
        Assert.True(UnclaimableChecker.Default.IsReserved("fuckwaffle"));
        Assert.True(UnclaimableChecker.Default.IsClaimable("facturatiehulp"));
        Assert.True(UnclaimableChecker.Default.IsClaimable("abrechnungshilfe"));
        Assert.True(UnclaimableChecker.Default.IsClaimable("motdepasseoublie"));
    }

    [Fact]
    public void AddingDutchKeepsEnglishEnabled()
    {
        var options = new UnclaimableOptions();
        options.AddLanguage(UnclaimableLanguage.Dutch);
        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("customersupport"));
        Assert.Equal("support", checker.Check("facturatiehulp").Category);
        Assert.Equal("roles", checker.Check("systeembeheerder").Category);
        Assert.Equal("system", checker.Check("wachtwoordvergeten").Category);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.True(checker.IsClaimable("abrechnungshilfe"));
    }

    [Fact]
    public void EnglishCanBeRemovedForDutchOnlyFiltering()
    {
        var options = new UnclaimableOptions();
        options.RemoveLanguage(UnclaimableLanguage.English);
        options.AddLanguage(UnclaimableLanguage.Dutch);
        var checker = new UnclaimableChecker(options);

        Assert.Equal("support", checker.Check("facturatiehulp").Category);
        Assert.True(checker.IsClaimable("customersupport"));
        Assert.True(checker.IsClaimable("abrechnungshilfe"));
    }

    [Theory]
    [InlineData(UnclaimableLanguage.German, "abrechnungshilfe", "systemverwalter", "passwortvergessen", "scheiße")]
    [InlineData(UnclaimableLanguage.French, "serviceclient", "administrateursysteme", "motdepasseoublie", "connard")]
    [InlineData(UnclaimableLanguage.Spanish, "servicioalcliente", "administradorsistema", "contrasenaolvidada", "gilipollas")]
    [InlineData(UnclaimableLanguage.Italian, "servizioclienti", "amministratoresistema", "passworddimenticata", "vaffanculo")]
    [InlineData(UnclaimableLanguage.Portuguese, "atendimentocliente", "administradorsistema", "senhaesquecida", "caralho")]
    public void LocalizedDatasetsCanBeAdded(
        UnclaimableLanguage language,
        string support,
        string role,
        string system,
        string profanity)
    {
        var options = new UnclaimableOptions();
        options.AddLanguage(language);
        var checker = new UnclaimableChecker(options);

        Assert.Equal("support", checker.Check(support).Category);
        Assert.Equal("roles", checker.Check(role).Category);
        Assert.Equal("system", checker.Check(system).Category);
        Assert.Equal("profanity", checker.Check(profanity).Category);
        Assert.True(checker.IsReserved("customersupport"));
    }

    [Fact]
    public void MultipleLanguagesAreAdditiveWithoutSeparateMode()
    {
        var options = new UnclaimableOptions();
        options.AddLanguage(UnclaimableLanguage.Dutch);
        options.AddLanguage(UnclaimableLanguage.German);
        options.AddLanguage(UnclaimableLanguage.French);
        options.AddLanguage(UnclaimableLanguage.Spanish);
        options.AddLanguage(UnclaimableLanguage.Italian);
        options.AddLanguage(UnclaimableLanguage.Portuguese);
        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("customersupport"));
        Assert.True(checker.IsReserved("facturatiehulp"));
        Assert.True(checker.IsReserved("abrechnungshilfe"));
        Assert.True(checker.IsReserved("serviceclient"));
        Assert.True(checker.IsReserved("servicioalcliente"));
        Assert.True(checker.IsReserved("servizioclienti"));
        Assert.True(checker.IsReserved("atendimentocliente"));
    }

    [Theory]
    [InlineData(UnclaimableLanguage.English)]
    [InlineData(UnclaimableLanguage.Dutch)]
    [InlineData(UnclaimableLanguage.German)]
    [InlineData(UnclaimableLanguage.French)]
    [InlineData(UnclaimableLanguage.Spanish)]
    [InlineData(UnclaimableLanguage.Italian)]
    [InlineData(UnclaimableLanguage.Portuguese)]
    public void GlobalDatasetsAreAlwaysIncluded(UnclaimableLanguage language)
    {
        var options = new UnclaimableOptions();
        options.Languages.Clear();
        options.AddLanguage(language);
        var checker = new UnclaimableChecker(options);

        Assert.Equal("brands", checker.Check("rituals").Category);
        Assert.Equal("technology", checker.Check("homeassistant").Category);
    }

    [Fact]
    public void NoLocalizedLanguagesStillKeepsGlobalDatasets()
    {
        var options = new UnclaimableOptions();
        options.Languages.Clear();
        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsClaimable("customersupport"));
        Assert.Equal("brands", checker.Check("rituals").Category);
        Assert.Equal("technology", checker.Check("homeassistant").Category);
    }

    [Fact]
    public void MixedLanguageValuesStillMatchEnabledProtectedSubstrings()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions());

        var result = checker.Check("klantenservice");

        Assert.True(result.IsReserved);
        Assert.Equal("service", result.MatchedValue);
        Assert.Equal("system", result.Category);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void InvalidLanguageFailsFast()
    {
        var options = new UnclaimableOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.AddLanguage((UnclaimableLanguage)999));

        options.Languages.Add((UnclaimableLanguage)999);
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnclaimableChecker(options));
    }
}
