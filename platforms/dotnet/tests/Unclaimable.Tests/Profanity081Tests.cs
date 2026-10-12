using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class Profanity081Tests
{
    private const Rule StructuralRules = Rule.MinimumLength | Rule.MaximumLength
        | Rule.Numbers | Rule.Whitespace | Rule.BlockedCharacters
        | Rule.LeadingSeparator | Rule.TrailingSeparator;

    public static IEnumerable<object[]> ExpandedExplicitTerms()
    {
        yield return new object[] { Language.English, "anus" };
        yield return new object[] { Language.English, "anuses" };
        yield return new object[] { Language.English, "arse" };
        yield return new object[] { Language.English, "ass" };
        yield return new object[] { Language.English, "boob" };
        yield return new object[] { Language.English, "boobs" };
        yield return new object[] { Language.English, "breast" };
        yield return new object[] { Language.English, "breasts" };
        yield return new object[] { Language.English, "cock" };
        yield return new object[] { Language.English, "cum" };
        yield return new object[] { Language.English, "cunt" };
        yield return new object[] { Language.English, "dick" };
        yield return new object[] { Language.English, "fuck" };
        yield return new object[] { Language.English, "penis" };
        yield return new object[] { Language.English, "penises" };
        yield return new object[] { Language.English, "pussy" };
        yield return new object[] { Language.English, "tit" };
        yield return new object[] { Language.English, "tits" };
        yield return new object[] { Language.English, "titties" };
        yield return new object[] { Language.English, "vagina" };
        yield return new object[] { Language.English, "vaginas" };
        yield return new object[] { Language.English, "vulva" };
        yield return new object[] { Language.English, "vulvas" };
        yield return new object[] { Language.English, "wank" };
        yield return new object[] { Language.Dutch, "piemel" };
        yield return new object[] { Language.Dutch, "piemels" };
        yield return new object[] { Language.Dutch, "tieten" };
        yield return new object[] { Language.Dutch, "kut" };
        yield return new object[] { Language.Dutch, "lul" };
        yield return new object[] { Language.Dutch, "neuk" };
        yield return new object[] { Language.Dutch, "neuken" };
        yield return new object[] { Language.Dutch, "neuker" };
        yield return new object[] { Language.Dutch, "kontneuken" };
        yield return new object[] { Language.Dutch, "reet" };
        yield return new object[] { Language.Dutch, "slet" };
        yield return new object[] { Language.Dutch, "snol" };
        yield return new object[] { Language.German, "pimmel" };
        yield return new object[] { Language.German, "titten" };
        yield return new object[] { Language.German, "schwanz" };
        yield return new object[] { Language.German, "schwengel" };
        yield return new object[] { Language.German, "klöten" };
        yield return new object[] { Language.German, "fotze" };
        yield return new object[] { Language.German, "möse" };
        yield return new object[] { Language.German, "fick" };
        yield return new object[] { Language.German, "ficken" };
        yield return new object[] { Language.German, "bumsen" };
        yield return new object[] { Language.German, "wichse" };
        yield return new object[] { Language.German, "wichsen" };
        yield return new object[] { Language.French, "pénis" };
        yield return new object[] { Language.French, "vagin" };
        yield return new object[] { Language.French, "vulve" };
        yield return new object[] { Language.French, "nichons" };
        yield return new object[] { Language.French, "bite" };
        yield return new object[] { Language.French, "teub" };
        yield return new object[] { Language.French, "zob" };
        yield return new object[] { Language.French, "chatte" };
        yield return new object[] { Language.French, "baise" };
        yield return new object[] { Language.French, "branler" };
        yield return new object[] { Language.French, "branlette" };
        yield return new object[] { Language.French, "niquer" };
        yield return new object[] { Language.French, "encule" };
        yield return new object[] { Language.Spanish, "pene" };
        yield return new object[] { Language.Spanish, "ano" };
        yield return new object[] { Language.Spanish, "tetas" };
        yield return new object[] { Language.Spanish, "polla" };
        yield return new object[] { Language.Spanish, "verga" };
        yield return new object[] { Language.Spanish, "pija" };
        yield return new object[] { Language.Spanish, "chota" };
        yield return new object[] { Language.Spanish, "coño" };
        yield return new object[] { Language.Spanish, "cojones" };
        yield return new object[] { Language.Spanish, "follar" };
        yield return new object[] { Language.Spanish, "chingar" };
        yield return new object[] { Language.Spanish, "pajearse" };
        yield return new object[] { Language.Italian, "tette" };
        yield return new object[] { Language.Italian, "vagine" };
        yield return new object[] { Language.Italian, "cazzo" };
        yield return new object[] { Language.Italian, "minchia" };
        yield return new object[] { Language.Italian, "fica" };
        yield return new object[] { Language.Italian, "figa" };
        yield return new object[] { Language.Italian, "inculare" };
        yield return new object[] { Language.Italian, "pompino" };
        yield return new object[] { Language.Italian, "sborra" };
        yield return new object[] { Language.Italian, "sborrare" };
        yield return new object[] { Language.Portuguese, "pênis" };
        yield return new object[] { Language.Portuguese, "ânus" };
        yield return new object[] { Language.Portuguese, "seios" };
        yield return new object[] { Language.Portuguese, "piroca" };
        yield return new object[] { Language.Portuguese, "buceta" };
        yield return new object[] { Language.Portuguese, "caralho" };
        yield return new object[] { Language.Portuguese, "colhões" };
        yield return new object[] { Language.Portuguese, "pica" };
        yield return new object[] { Language.Portuguese, "xereca" };
        yield return new object[] { Language.Portuguese, "xoxota" };
        yield return new object[] { Language.Portuguese, "foda" };
        yield return new object[] { Language.Portuguese, "foder" };
        yield return new object[] { Language.Portuguese, "boquete" };
        yield return new object[] { Language.Portuguese, "porra" };
        yield return new object[] { Language.Portuguese, "punheta" };
        yield return new object[] { Language.Portuguese, "esporra" };
        yield return new object[] { Language.Polish, "pochwa" };
        yield return new object[] { Language.Polish, "srom" };
        yield return new object[] { Language.Polish, "odbyt" };
        yield return new object[] { Language.Polish, "cycki" };
        yield return new object[] { Language.Polish, "chuj" };
        yield return new object[] { Language.Polish, "cipa" };
        yield return new object[] { Language.Polish, "kutas" };
        yield return new object[] { Language.Polish, "pizda" };
        yield return new object[] { Language.Polish, "jebać" };
        yield return new object[] { Language.Polish, "pierdolić" };
        yield return new object[] { Language.Turkish, "vajina" };
        yield return new object[] { Language.Turkish, "anüs" };
        yield return new object[] { Language.Turkish, "göt" };
        yield return new object[] { Language.Turkish, "amcık" };
        yield return new object[] { Language.Turkish, "yarrak" };
        yield return new object[] { Language.Turkish, "sikmek" };
        yield return new object[] { Language.Turkish, "siktir" };
        yield return new object[] { Language.Indonesian, "payudara" };
        yield return new object[] { Language.Indonesian, "kontol" };
        yield return new object[] { Language.Indonesian, "memek" };
        yield return new object[] { Language.Indonesian, "ngentot" };
        yield return new object[] { Language.Czech, "vagína" };
        yield return new object[] { Language.Czech, "prsa" };
        yield return new object[] { Language.Czech, "kokot" };
        yield return new object[] { Language.Czech, "píča" };
        yield return new object[] { Language.Czech, "čurák" };
        yield return new object[] { Language.Vietnamese, "dương vật" };
        yield return new object[] { Language.Vietnamese, "duong vat" };
        yield return new object[] { Language.Vietnamese, "dươngvật" };
        yield return new object[] { Language.Vietnamese, "duongvat" };
        yield return new object[] { Language.Vietnamese, "âm đạo" };
        yield return new object[] { Language.Vietnamese, "am dao" };
        yield return new object[] { Language.Vietnamese, "âmđạo" };
        yield return new object[] { Language.Vietnamese, "amdao" };
        yield return new object[] { Language.Vietnamese, "âm hộ" };
        yield return new object[] { Language.Vietnamese, "am ho" };
        yield return new object[] { Language.Vietnamese, "âmhộ" };
        yield return new object[] { Language.Vietnamese, "amho" };
        yield return new object[] { Language.Vietnamese, "hậu môn" };
        yield return new object[] { Language.Vietnamese, "hau mon" };
        yield return new object[] { Language.Vietnamese, "hậumôn" };
        yield return new object[] { Language.Vietnamese, "haumon" };
        yield return new object[] { Language.Vietnamese, "buồi" };
        yield return new object[] { Language.Vietnamese, "cặc" };
        yield return new object[] { Language.Vietnamese, "lồn" };
        yield return new object[] { Language.Vietnamese, "địt mẹ" };
        yield return new object[] { Language.Vietnamese, "đụ má" };
        yield return new object[] { Language.Hungarian, "pénisz" };
        yield return new object[] { Language.Hungarian, "penisz" };
        yield return new object[] { Language.Hungarian, "ánusz" };
        yield return new object[] { Language.Hungarian, "anusz" };
        yield return new object[] { Language.Hungarian, "cici" };
        yield return new object[] { Language.Hungarian, "cicik" };
        yield return new object[] { Language.Hungarian, "fasz" };
        yield return new object[] { Language.Hungarian, "geci" };
        yield return new object[] { Language.Hungarian, "picsa" };
        yield return new object[] { Language.Hungarian, "pina" };
        yield return new object[] { Language.Hungarian, "baszd meg" };
        yield return new object[] { Language.Swedish, "tuttar" };
        yield return new object[] { Language.Swedish, "tutte" };
        yield return new object[] { Language.Swedish, "bröst" };
        yield return new object[] { Language.Swedish, "fitta" };
        yield return new object[] { Language.Swedish, "knulla" };
        yield return new object[] { Language.Swedish, "kuk" };
        yield return new object[] { Language.Swedish, "rövhål" };
        yield return new object[] { Language.Romanian, "penisuri" };
        yield return new object[] { Language.Romanian, "vulvă" };
        yield return new object[] { Language.Romanian, "sâni" };
        yield return new object[] { Language.Romanian, "sani" };
        yield return new object[] { Language.Romanian, "țâțe" };
        yield return new object[] { Language.Romanian, "curvă" };
        yield return new object[] { Language.Romanian, "muie" };
        yield return new object[] { Language.Romanian, "pizdă" };
        yield return new object[] { Language.Romanian, "pulă" };
        yield return new object[] { Language.Romanian, "sugi pula" };
    }

    [Fact]
    public void MultilingualProfanityMatchingIsEnabledByDefault()
    {
        Assert.True(new Options().MultilingualProfanityMatching);
    }

    [Theory]
    [MemberData(nameof(ExpandedExplicitTerms))]
    public void EveryExpandedExplicitTermWorksInItsOwnLanguageFilter(Language language, string value)
    {
        var options = CreateProfanityOnlyOptions(multilingual: false);
        options.AddLanguage(language);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved, value);
        Assert.Equal("profanity", result.Category);
    }

    [Theory]
    [MemberData(nameof(ExpandedExplicitTerms))]
    public void EveryExpandedExplicitTermWorksInMultilingualMode(Language language, string value)
    {
        var options = CreateProfanityOnlyOptions(multilingual: true);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved, $"{language}: {value}");
        Assert.Equal("profanity", result.Category);
    }

    [Fact]
    public void EveryProfanityDatasetEntryWorksInMultilingualMode()
    {
        var options = CreateProfanityOnlyOptions(multilingual: true);
        options.DisablePattern(
            Pattern.NumericOnly
            | Pattern.Repeated
            | Pattern.SymbolOnly
            | Pattern.AsciiArt
            | Pattern.UppercaseOnly);

        var checker = new Checker(options);
        var count = 0;

        foreach (var value in ReadAllProfanityEntries())
        {
            var result = checker.Check(value);

            Assert.True(
                result.IsReserved && result.Category == "profanity",
                $"Multilingual profanity missed '{value}' or classified it as {result.MatchKind}/{result.Category ?? "<none>"}.");
            Assert.True(
                result.MatchKind == MatchKind.Exact,
                $"Expected exact profanity match for '{value}', got {result.MatchKind}.");
            count++;
        }

        Assert.True(count >= 1300, $"Profanity sweep unexpectedly small: {count} entries.");
    }

    [Fact]
    public void DisablingMultilingualModeKeepsSelectedEnglishProfanityActive()
    {
        var options = new Options { MultilingualProfanityMatching = false };
        var checker = new Checker(options);

        Assert.Equal("profanity", checker.Check("fuck").Category);
        Assert.Equal("profanity", checker.Check("penis").Category);
        Assert.True(checker.IsClaimable("godverdomme"));
        Assert.True(checker.IsClaimable("piemel"));
    }

    [Fact]
    public void AddingDutchWithMultilingualModeDisabledEnablesDutchProfanity()
    {
        var options = new Options { MultilingualProfanityMatching = false };
        options.AddLanguage(Language.Dutch);

        var checker = new Checker(options);

        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.Equal("profanity", checker.Check("piemel").Category);
        Assert.Equal("profanity", checker.Check("fuck").Category);
    }

    [Fact]
    public void MultilingualModeUsesUnselectedLanguageProfanity()
    {
        var checker = new Checker(new Options());

        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.Equal("profanity", checker.Check("piemel").Category);
        Assert.Equal("profanity", checker.Check("pimmel").Category);
    }

    [Fact]
    public void MultilingualProfanityLanguageOverrideLimitsOnlyExtraProfanityLanguages()
    {
        var options = new Options()
            .SetMultilingualProfanityLanguages(Language.Dutch, Language.German);

        var checker = new Checker(options);

        Assert.Equal("profanity", checker.Check("fuck").Category);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.Equal("profanity", checker.Check("pimmel").Category);
        Assert.True(checker.IsClaimable("putain"));
        Assert.True(checker.IsClaimable("facturatiehulp"));
    }

    [Fact]
    public void NormallySelectedLanguageStillProvidesProfanityOutsideMultilingualOverride()
    {
        var options = new Options()
            .SetMultilingualProfanityLanguages(Language.Dutch);

        options.AddLanguage(Language.French);
        var checker = new Checker(options);

        Assert.Equal("profanity", checker.Check("putain").Category);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.True(checker.IsClaimable("pimmel"));
    }

    [Fact]
    public void EmptyMultilingualProfanityOverrideRestoresAllSupportedLanguages()
    {
        var options = new Options()
            .SetMultilingualProfanityLanguages(Language.Dutch);

        options.SetMultilingualProfanityLanguages();

        var checker = new Checker(options);

        Assert.Equal(
            Enum.GetValues<Language>().OrderBy(language => language),
            options.MultilingualProfanityLanguages.OrderBy(language => language));
        Assert.Equal("profanity", checker.Check("fuck").Category);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.Equal("profanity", checker.Check("pimmel").Category);
        Assert.Equal("profanity", checker.Check("putain").Category);
    }

    [Fact]
    public void MultilingualProfanityLanguageOverrideDefaultsToAllSupportedLanguages()
    {
        var options = new Options();

        Assert.Equal(
            Enum.GetValues<Language>().OrderBy(language => language),
            options.MultilingualProfanityLanguages.OrderBy(language => language));
    }

    [Fact]
    public void MultilingualProfanityLanguageOverrideRejectsUnsupportedLanguageValues()
    {
        var options = new Options();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => options.SetMultilingualProfanityLanguages((Language)999));
    }

    [Fact]
    public void MultilingualProfanityLanguageOverrideRejectsNull()
    {
        var options = new Options();

        Assert.Throws<ArgumentNullException>(
            () => options.SetMultilingualProfanityLanguages(null!));
    }

    [Theory]
    [InlineData("p3n1s", "penis")]
    [InlineData("b00bs", "boobs")]
    [InlineData("v4g1n4", "vagina")]
    [InlineData("t1ts", "tits")]
    public void ExplicitTermsUseExistingObfuscationPipeline(string value, string expected)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.True(result.IsReserved, value);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(expected, result.MatchedValue);
        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
    }

    [Theory]
    [InlineData("myb00bname", "boob")]
    [InlineData("xxpenisxx", "penis")]
    [InlineData("myp3n1sname", "penis")]
    [InlineData("xxv4g1n4xx", "vagina")]
    [InlineData("mypiemelname", "piemel")]
    public void CuratedExplicitTermsBlockWrappedAndObfuscatedWrappedForms(string value, string expected)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.True(result.IsReserved, value);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(expected, result.MatchedValue);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Theory]
    [InlineData("cocktail")]
    [InlineData("penelope")]
    [InlineData("janus")]
    public void OtherCollisionProneExplicitTermsRemainExactOnlyByDefault(string value)
    {
        Assert.True(new Checker(new Options()).IsClaimable(value), value);
    }

    [Fact]
    public void RemovingLanguagesDoesNotDisableMultilingualProfanity()
    {
        var options = new Options();
        foreach (var language in Enum.GetValues<Language>())
        {
            options.RemoveLanguage(language);
        }

        var checker = new Checker(options);

        Assert.Empty(options.Languages);
        Assert.Equal("profanity", checker.Check("godverdomme").Category);
        Assert.Equal("profanity", checker.Check("piemel").Category);
        Assert.Equal("profanity", checker.Check("pimmel").Category);
    }

    [Fact]
    public void RemovingLanguagesAndDisablingMultilingualModeDisablesLocalizedProfanity()
    {
        var options = new Options { MultilingualProfanityMatching = false };
        foreach (var language in Enum.GetValues<Language>())
        {
            options.RemoveLanguage(language);
        }

        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("penis"));
        Assert.True(checker.IsClaimable("piemel"));
        Assert.True(checker.IsClaimable("godverdomme"));
    }

    [Fact]
    public void CompatibilityProfanityToggleStillDisablesAllProfanity()
    {
        var options = new Options
        {
            ProfanityMatching = false
        };

        Assert.True(new Checker(options).IsClaimable("penis"));
        Assert.True(new Checker(options).IsClaimable("godverdomme"));
    }

    [Fact]
    public void ProfanityCanStillBeDisabledByCategory()
    {
        var options = new Options();
        options.DisableCategory(Category.Profanity);

        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("penis"));
        Assert.True(checker.IsClaimable("piemel"));
        Assert.True(checker.IsClaimable("godverdomme"));
    }

    [Fact]
    public void ProfanityCanStillBeDisabledByRule()
    {
        var options = new Options();
        options.DisableRule(Rule.Profanity);

        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("penis"));
        Assert.True(checker.IsClaimable("piemel"));
        Assert.True(checker.IsClaimable("godverdomme"));
    }

    private static IEnumerable<string> ReadAllProfanityEntries()
    {
        var assembly = typeof(Checker).Assembly;

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            if (!root.TryGetProperty("category", out var category)
                || category.GetString() != "profanity")
            {
                continue;
            }

            if (root.TryGetProperty("values", out var values))
            {
                foreach (var value in values.EnumerateArray())
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        yield return text;
                    }
                }
            }

            if (root.TryGetProperty("partialValues", out var partialValues))
            {
                foreach (var value in partialValues.EnumerateArray())
                {
                    var text = value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        yield return text;
                    }
                }
            }

            if (!root.TryGetProperty("combinations", out var combinations))
            {
                continue;
            }

            foreach (var combination in combinations.EnumerateArray())
            {
                if (!combination.TryGetProperty("roots", out var roots)
                    || !combination.TryGetProperty("suffixes", out var suffixes))
                {
                    continue;
                }

                var suffixValues = suffixes
                    .EnumerateArray()
                    .Select(value => value.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Cast<string>()
                    .ToArray();

                foreach (var rootValue in roots.EnumerateArray())
                {
                    var rootText = rootValue.GetString();
                    if (string.IsNullOrWhiteSpace(rootText))
                    {
                        continue;
                    }

                    foreach (var suffix in suffixValues)
                    {
                        yield return rootText + suffix;
                    }
                }
            }
        }
    }

    private static Options CreateProfanityOnlyOptions(bool multilingual)
    {
        var options = new Options
        {
            MultilingualProfanityMatching = multilingual,
            DisabledRules = StructuralRules
        };

        foreach (var language in Enum.GetValues<Language>())
        {
            options.RemoveLanguage(language);
        }

        foreach (var category in Enum.GetValues<Category>())
        {
            if (category != Category.Profanity)
            {
                options.DisableCategory(category);
            }
        }

        return options;
    }
}
