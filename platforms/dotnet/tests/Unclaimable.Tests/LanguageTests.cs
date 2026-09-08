using Xunit;

namespace Unclaimable.Tests;

public sealed class LanguageTests
{
    [Fact]
    public void EnglishIsEnabledByDefault()
    {
        var options = new Options();

        Assert.Contains(Language.English, options.Languages);
        Assert.Single(options.Languages);
        Assert.True(Checker.Default.IsReserved("customersupport"));
        Assert.True(Checker.Default.IsReserved("fuckwaffle"));
        Assert.True(Checker.Default.IsClaimable("facturatiehulp"));
        Assert.True(Checker.Default.IsClaimable("abrechnungshilfe"));
        Assert.True(Checker.Default.IsClaimable("motdepasseoublie"));
    }

    [Fact]
    public void AddingDutchKeepsEnglishEnabled()
    {
        var options = new Options();
        options.AddLanguage(Language.Dutch);
        var checker = new Checker(options);

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
        var options = new Options();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(Language.Dutch);
        var checker = new Checker(options);

        Assert.Equal("support", checker.Check("facturatiehulp").Category);
        Assert.True(checker.IsClaimable("customersupport"));
        Assert.True(checker.IsClaimable("abrechnungshilfe"));
    }

    [Theory]
    [InlineData(Language.German, "abrechnungshilfe", "systemverwalter", "passwortvergessen", "scheiße")]
    [InlineData(Language.French, "serviceclient", "administrateursysteme", "motdepasseoublie", "connard")]
    [InlineData(Language.Spanish, "servicioalcliente", "administradorsistema", "contrasenaolvidada", "gilipollas")]
    [InlineData(Language.Italian, "servizioclienti", "amministratoresistema", "passworddimenticata", "vaffanculo")]
    [InlineData(Language.Portuguese, "atendimentocliente", "administradorsistema", "senhaesquecida", "caralho")]
    public void LocalizedDatasetsCanBeAdded(
        Language language,
        string support,
        string role,
        string system,
        string profanity)
    {
        var options = new Options();
        options.AddLanguage(language);
        var checker = new Checker(options);

        Assert.Equal("support", checker.Check(support).Category);
        Assert.Equal("roles", checker.Check(role).Category);
        Assert.Equal("system", checker.Check(system).Category);
        Assert.Equal("profanity", checker.Check(profanity).Category);
        Assert.True(checker.IsReserved("customersupport"));
    }

    [Fact]
    public void MultipleLanguagesAreAdditiveWithoutSeparateMode()
    {
        var options = new Options();
        options.AddLanguage(Language.Dutch);
        options.AddLanguage(Language.German);
        options.AddLanguage(Language.French);
        options.AddLanguage(Language.Spanish);
        options.AddLanguage(Language.Italian);
        options.AddLanguage(Language.Portuguese);
        var checker = new Checker(options);

        Assert.True(checker.IsReserved("customersupport"));
        Assert.True(checker.IsReserved("facturatiehulp"));
        Assert.True(checker.IsReserved("abrechnungshilfe"));
        Assert.True(checker.IsReserved("serviceclient"));
        Assert.True(checker.IsReserved("servicioalcliente"));
        Assert.True(checker.IsReserved("servizioclienti"));
        Assert.True(checker.IsReserved("atendimentocliente"));
    }

    [Theory]
    [InlineData(Language.English)]
    [InlineData(Language.Dutch)]
    [InlineData(Language.German)]
    [InlineData(Language.French)]
    [InlineData(Language.Spanish)]
    [InlineData(Language.Italian)]
    [InlineData(Language.Portuguese)]
    public void GlobalDatasetsAreAlwaysIncluded(Language language)
    {
        var options = new Options();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(language);
        var checker = new Checker(options);

        Assert.Equal("brands", checker.Check("rituals").Category);
        Assert.Equal("technology", checker.Check("homeassistant").Category);
    }

    [Fact]
    public void RemovingDefaultEnglishStillKeepsGlobalDatasets()
    {
        var options = new Options();
        options.RemoveLanguage(Language.English);
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("customersupport"));
        Assert.Equal("brands", checker.Check("rituals").Category);
        Assert.Equal("technology", checker.Check("homeassistant").Category);
    }

    [Fact]
    public void MixedLanguageValuesStillMatchEnabledProtectedSubstrings()
    {
        var checker = new Checker(new Options());

        var result = checker.Check("klantenservice");

        Assert.True(result.IsReserved);
        Assert.Equal("service", result.MatchedValue);
        Assert.Equal("system", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void InvalidLanguageFailsFast()
    {
        var options = new Options();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.AddLanguage((Language)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.RemoveLanguage((Language)999));
    }
}
