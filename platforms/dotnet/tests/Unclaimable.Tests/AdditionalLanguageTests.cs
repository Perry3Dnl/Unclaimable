using System.Text;
using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class AdditionalLanguageTests
{
    private const Rule StructuralRules = Rule.MinimumLength | Rule.MaximumLength
        | Rule.Numbers | Rule.Whitespace | Rule.BlockedCharacters
        | Rule.LeadingSeparator | Rule.TrailingSeparator;

    /// <summary>Provides a distinct support and profanity example for each new language.</summary>
    public static IEnumerable<object[]> LanguageCases()
    {
        yield return new object[] { Language.Polish, "pl", "obsługa klienta", "skurwysyn" };
        yield return new object[] { Language.Turkish, "tr", "müşteri hizmetleri", "pezevenk" };
        yield return new object[] { Language.Indonesian, "id", "layanan pelanggan", "bangsat" };
        yield return new object[] { Language.Czech, "cs", "zákaznický servis", "zkurvysyn" };
        yield return new object[] { Language.Vietnamese, "vi", "dịch vụ khách hàng", "thằng khốn" };
        yield return new object[] { Language.Hungarian, "hu", "ügyfélszolgálat", "rohadék" };
        yield return new object[] { Language.Swedish, "sv", "kundtjänst", "skitstövel" };
        yield return new object[] { Language.Romanian, "ro", "serviciul clienți", "sugi pula" };
    }

    /// <summary>Verifies that every embedded value loads under its own language and category.</summary>
    [Theory]
    [MemberData(nameof(LanguageCases))]
    public void EveryNewDatasetValueIsRecognized(
        Language language, string code, string support, string profanity)
    {
        var options = CreateOptions();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(language);
        var checker = new Checker(options);
        var categories = new HashSet<string>();
        var count = 0;

        foreach (var entry in ReadEntries(code))
        {
            var result = checker.Check(entry.Value);
            Assert.True(result.IsReserved, $"Missing {code}/{entry.Category}: {entry.Value}");
            Assert.Equal(entry.Category, result.Category);
            Assert.Equal(MatchKind.Exact, result.MatchKind);
            categories.Add(entry.Category);
            count++;
        }

        Assert.Equal(new[] { "profanity", "roles", "support", "system" }, categories.OrderBy(value => value));
        Assert.True(count >= 80, $"Incomplete language pack: {code}");
        Assert.Equal("support", checker.Check(support).Category);
        Assert.Equal("profanity", checker.Check(profanity).Category);
    }

    /// <summary>Verifies opt-in loading, additive English support and removal of a new language.</summary>
    [Theory]
    [MemberData(nameof(LanguageCases))]
    public void NewLanguageCanBeAddedAndRemoved(
        Language language, string code, string support, string profanity)
    {
        var options = CreateOptions();
        Assert.True(new Checker(options).IsClaimable(support), code);

        options.AddLanguage(language);
        var enabled = new Checker(options);
        Assert.Equal("support", enabled.Check(support).Category);
        Assert.True(enabled.IsReserved("customersupport"));

        options.RemoveLanguage(language);
        Assert.True(new Checker(options).IsClaimable(support));
        Assert.Contains(Language.English, options.Languages);
    }

    /// <summary>Verifies compact matching and canonical Unicode normalization for new packs.</summary>
    [Theory]
    [MemberData(nameof(LanguageCases))]
    public void NativeAccentsAndSeparatorsAreSupported(
        Language language, string code, string support, string profanity)
    {
        var options = CreateOptions();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(language);
        var checker = new Checker(options);

        Assert.Equal("support", checker.Check(support.Normalize(NormalizationForm.FormD)).Category);
        Assert.Equal("support", checker.Check(support.Replace(" ", "_")).Category);
        Assert.Equal("profanity", checker.Check(profanity.Normalize(NormalizationForm.FormD)).Category);
        Assert.Single(options.Languages);
        Assert.NotEmpty(code);
    }

    /// <summary>Verifies that disabling profanity preserves the language's other filter sets.</summary>
    [Theory]
    [MemberData(nameof(LanguageCases))]
    public void NewProfanityCanBeDisabledIndependently(
        Language language, string code, string support, string profanity)
    {
        var options = CreateOptions();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(language);
        options.DisabledRules |= Rule.Profanity;
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable(profanity), code);
        Assert.Equal("support", checker.Check(support).Category);
    }

    /// <summary>Checks that all supported languages can coexist without rejecting ordinary personal names.</summary>
    [Fact]
    public void AllFifteenLanguagesCanBeEnabledTogether()
    {
        var options = CreateOptions();
        options.Strictness = Strictness.Strict;
        foreach (var language in Enum.GetValues<Language>())
        {
            options.AddLanguage(language);
        }

        Assert.Equal(15, options.Languages.Count);
        var checker = new Checker(options);
        foreach (var row in LanguageCases())
        {
            Assert.True(checker.IsReserved((string)row[2]));
        }

        foreach (var name in new[] { "Agnieszka", "Deniz", "Bintang", "Tereza", "Minh Anh", "Eszter", "Astrid", "Andreea" })
        {
            Assert.True(checker.IsClaimable(name), name);
        }
    }

    /// <summary>Preserves the numeric identifiers of previously published language choices.</summary>
    [Fact]
    public void ExistingLanguageIdentifiersRemainStable()
    {
        Assert.Equal(0, (int)Language.English);
        Assert.Equal(1, (int)Language.Dutch);
        Assert.Equal(2, (int)Language.German);
        Assert.Equal(3, (int)Language.French);
        Assert.Equal(4, (int)Language.Spanish);
        Assert.Equal(5, (int)Language.Italian);
        Assert.Equal(6, (int)Language.Portuguese);
    }

    private static Options CreateOptions() => new Options
    {
        Strictness = Strictness.Standard,
        DisabledRules = StructuralRules
    };

    private static IEnumerable<(string Category, string Value)> ReadEntries(string code)
    {
        var assembly = typeof(Checker).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                || !resource.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            if (!root.TryGetProperty("language", out var language)
                || language.GetString() != code)
            {
                continue;
            }

            var category = root.GetProperty("category").GetString()!;
            foreach (var value in root.GetProperty("values").EnumerateArray())
            {
                yield return (category, value.GetString()!);
            }
        }
    }
}
