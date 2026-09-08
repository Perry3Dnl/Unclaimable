using System.Text;
using Xunit;

namespace Unclaimable.Tests;

public sealed class FilterBypassTests
{
    private const Rule StructuralRules = Rule.MinimumLength | Rule.MaximumLength
        | Rule.Numbers | Rule.Whitespace | Rule.BlockedCharacters
        | Rule.LeadingSeparator | Rule.TrailingSeparator;

    /// <summary>Provides independent bypass attempts for every built-in dataset category.</summary>
    public static IEnumerable<object[]> CategoryCases()
    {
        yield return new object[] { "roles", "admin", "adm1n", "аdmin", "аdm1n" };
        yield return new object[] { "support", "support", "supp0rt", "suppоrt", "suppоr7" };
        yield return new object[] { "system", "system", "sys7em", "sуstem", "sуs7em" };
        yield return new object[] { "brands", "nike", "n1ke", "nіke", "nіk3" };
        yield return new object[] { "technology", "google", "g00gle", "gоogle", "gо0gle" };
        yield return new object[] { "profanity", "shit", "sh1t", "shіt", "ѕh1t" };
    }

    /// <summary>Attempts casing, separator, invisible-character, width, leetspeak and lookalike bypasses.</summary>
    [Theory]
    [MemberData(nameof(CategoryCases))]
    public void EveryCategoryRejectsObfuscatedWholeNames(
        string category, string value, string leet, string lookalike, string combined)
    {
        var checker = new Checker(MatchingOptions());
        var attempts = new[]
        {
            value.ToUpperInvariant(),
            string.Join(".", value.ToCharArray()),
            string.Join("_", value.ToCharArray()),
            string.Join("\t", value.ToCharArray()),
            string.Join("\u200B", value.ToCharArray()),
            string.Join("\u200D", value.ToCharArray()),
            string.Join("\u2060", value.ToCharArray()),
            string.Join("\uFEFF", value.ToCharArray()),
            "\u00A0" + value + "\u00A0",
            new string(value.Select(character => (char)(character + 0xFEE0)).ToArray()),
            value.Insert(1, "\u0301"),
            leet,
            lookalike,
            string.Join("-", combined.ToCharArray())
        };

        foreach (var attempt in attempts)
        {
            AssertCategory(checker, attempt, category);
        }
    }

    /// <summary>Attempts prefix and suffix camouflage around every category with partial matching enabled.</summary>
    [Theory]
    [MemberData(nameof(CategoryCases))]
    public void EveryCategoryRejectsEmbeddedObfuscatedNames(
        string category, string value, string leet, string lookalike, string combined)
    {
        var options = MatchingOptions();
        options.Strictness = Strictness.Strict;
        options.ProfanityPartialMatching = true;
        var checker = new Checker(options);

        foreach (var attempt in new[] { value, leet, lookalike, combined })
        {
            AssertCategory(checker, "qq" + attempt + "zz", category);
            AssertCategory(checker, "qq." + string.Join(".", attempt.ToCharArray()) + ".zz", category);
        }
    }

    /// <summary>Checks that each matching rule can be isolated, enabled and disabled intentionally.</summary>
    [Theory]
    [InlineData(Rule.CompactMatching, "a.d.m.i.n", "roles")]
    [InlineData(Rule.ObfuscationMatching, "adm1n", "roles")]
    [InlineData(Rule.UnicodeConfusableMatching, "аdmin", "roles")]
    [InlineData(Rule.PartialMatching, "qqadminzz", "roles")]
    [InlineData(Rule.Profanity, "shit", "profanity")]
    public void MatchingRuleSwitchesHaveObservableEffect(Rule rule, string input, string category)
    {
        var options = MatchingOptions();
        options.DisabledRules |= Rule.CompactMatching | Rule.ObfuscationMatching
            | Rule.UnicodeConfusableMatching | Rule.PartialMatching | Rule.Profanity;
        options.PartialMatching = true;
        Assert.True(new Checker(options).IsClaimable(input));

        options.DisabledRules &= ~rule;
        AssertCategory(new Checker(options), input, category);
        options.DisabledRules |= rule;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks obfuscation of application-specific reservations without relying on built-in words.</summary>
    [Theory]
    [InlineData("QUARTZKEEPER")]
    [InlineData("q.u.a.r.t.z.k.e.e.p.e.r")]
    [InlineData("qu4rtzk33per")]
    [InlineData("quаrtzkeeper")]
    [InlineData("qqquаrtzk33perzz")]
    public void CustomReservationsRejectBypasses(string input)
    {
        var options = MatchingOptions();
        options.Strictness = Strictness.Strict;
        options.AdditionalReserved.Add("quartzkeeper");
        AssertCategory(new Checker(options), input, "custom");
    }

    /// <summary>Checks Unicode decimal digits from multiple scripts and supplementary planes.</summary>
    [Theory]
    [InlineData("0")]
    [InlineData("\u0661")]
    [InlineData("\u06F2")]
    [InlineData("\u0969")]
    [InlineData("\uFF14")]
    [InlineData("\U0001D7D3")]
    public void NumberRuleRejectsUnicodeDigitBypasses(string digit)
    {
        var options = MatchingOptions();
        options.DisabledRules &= ~Rule.Numbers;
        var input = "qz" + digit + "vx";
        var result = new Checker(options).Check(input);
        Assert.Equal(MatchKind.NumbersNotAllowed, result.MatchKind);
        Assert.Equal(digit, result.OffendingCharacter);
        Assert.Equal(2, result.OffendingCharacterIndex);

        options.DisabledRules |= Rule.Numbers;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks whitespace that can be visually confused with an ordinary space or omitted.</summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u00A0")]
    [InlineData("\u2007")]
    [InlineData("\u202F")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    [InlineData("\u3000")]
    public void WhitespaceRuleRejectsHiddenWhitespace(string whitespace)
    {
        var options = MatchingOptions();
        options.DisabledRules &= ~Rule.Whitespace;
        var input = "qz" + whitespace + "vx";
        var result = new Checker(options).Check(input);
        Assert.Equal(MatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal(whitespace, result.OffendingCharacter);

        options.DisabledRules |= Rule.Whitespace;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks that edge-separator rules survive relaxation of interior punctuation rules.</summary>
    [Theory]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData(".")]
    public void EdgeSeparatorsCannotBypassIndependentRules(string separator)
    {
        var options = MatchingOptions();
        options.DisabledRules &= ~(Rule.LeadingSeparator | Rule.TrailingSeparator);
        var checker = new Checker(options);
        Assert.Equal(MatchKind.LeadingSeparator, checker.Check(separator + "qzvx").MatchKind);
        Assert.Equal(MatchKind.TrailingSeparator, checker.Check("qzvx" + separator).MatchKind);
        Assert.True(checker.IsClaimable("qz" + separator + "vx"));

        options.DisabledRules |= Rule.LeadingSeparator | Rule.TrailingSeparator;
        checker = new Checker(options);
        Assert.True(checker.IsClaimable(separator + "qzvx"));
        Assert.True(checker.IsClaimable("qzvx" + separator));
    }

    /// <summary>Checks application-specific blocked Unicode scalars and runtime policy changes.</summary>
    [Theory]
    [InlineData("^")]
    [InlineData("\u200B")]
    [InlineData("\U0001F680")]
    public void BlockedCharactersCannotBypassRuntimePolicy(string character)
    {
        var options = MatchingOptions();
        options.DisabledRules &= ~Rule.BlockedCharacters;
        options.AdditionalBlockedCharacters(character);
        var policy = new Policy(options.ConfiguredBlockedCharacters);
        var checker = new Checker(options, policy);
        var input = "qz" + character + "vx";

        Assert.Equal(MatchKind.BlockedCharacter, checker.Check(input).MatchKind);
        policy.AllowCharacter(character);
        Assert.True(checker.IsClaimable(input));
        policy.BlockCharacter(character);
        var result = checker.Check(input);
        Assert.Equal(MatchKind.BlockedCharacter, result.MatchKind);
        Assert.Equal(character, result.OffendingCharacter);

        options.DisabledRules |= Rule.BlockedCharacters;
        Assert.True(new Checker(options, policy).IsClaimable(input));
    }

    /// <summary>Checks both length boundaries and verifies checks run before width normalization.</summary>
    [Theory]
    [InlineData("qz", MatchKind.TooShort)]
    [InlineData("qzv", MatchKind.None)]
    [InlineData("qzvxmn", MatchKind.None)]
    [InlineData("qzvxmnp", MatchKind.TooLong)]
    [InlineData("ｑｚ", MatchKind.TooShort)]
    [InlineData("ｑｚｖｘｍｎｐ", MatchKind.TooLong)]
    public void LengthRulesEnforceConfiguredBoundaries(string input, MatchKind expected)
    {
        var options = MatchingOptions();
        options.MinimumLength = 3;
        options.MaximumLength = 6;
        options.DisabledRules &= ~(Rule.MinimumLength | Rule.MaximumLength);
        Assert.Equal(expected, new Checker(options).Check(input).MatchKind);
        options.DisabledRules |= Rule.MinimumLength | Rule.MaximumLength;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks ASCII-only validation before compatibility normalization can hide non-ASCII input.</summary>
    [Theory]
    [InlineData("qzévx")]
    [InlineData("ｑｚｖｘ")]
    [InlineData("qz\u200Bvx")]
    [InlineData("qz\U0001F680vx")]
    public void AsciiOnlyRejectsNonAsciiBypasses(string input)
    {
        var options = MatchingOptions();
        options.AsciiOnly = true;
        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(input).MatchKind);
        options.AsciiOnly = false;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks that removing accents from native reserved words cannot bypass Unicode matching.</summary>
    [Theory]
    [InlineData(Language.Turkish, "yonetici", "roles")]
    [InlineData(Language.Czech, "prihlaseni", "system")]
    [InlineData(Language.Vietnamese, "dichvukhachhang", "support")]
    [InlineData(Language.Swedish, "losenord", "system")]
    [InlineData(Language.Romanian, "pizda", "profanity")]
    public void AccentOmissionCannotBypassNativeReservedWords(Language language, string input, string category)
    {
        var options = MatchingOptions();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(language);
        AssertCategory(new Checker(options), input, category);
        options.DisabledRules |= Rule.UnicodeConfusableMatching;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks combinations of accent omission, leetspeak and partial matching for native reservations.</summary>
    [Theory]
    [InlineData("zephyrium", false, MatchKind.UnicodeConfusable)]
    [InlineData("z.e.p.h.y.r.i.u.m", false, MatchKind.UnicodeConfusable)]
    [InlineData("z3phyrium", false, MatchKind.Obfuscated)]
    [InlineData("qqzephyriumzz", true, MatchKind.Partial)]
    [InlineData("qqz3phyriumzz", true, MatchKind.Partial)]
    public void NativeReservationAliasesHonorMatchingControls(string input, bool partial, MatchKind expected)
    {
        var options = MatchingOptions();
        options.PartialMatching = partial;
        options.AdditionalReserved.Add("zéphyrium");
        var result = new Checker(options).Check(input);
        Assert.True(result.IsReserved);
        Assert.Equal("custom", result.Category);
        Assert.Equal("zéphyrium", result.MatchedValue);
        Assert.Equal(expected, result.MatchKind);

        options.DisabledRules |= Rule.UnicodeConfusableMatching;
        Assert.True(new Checker(options).IsClaimable(input));
    }

    /// <summary>Checks that native profanity aliases still require the separate profanity-partial option.</summary>
    [Fact]
    public void NativeProfanityAliasesPreservePartialOptIn()
    {
        var options = MatchingOptions();
        options.RemoveLanguage(Language.English);
        options.AddLanguage(Language.Romanian);
        options.Strictness = Strictness.Strict;
        Assert.True(new Checker(options).IsClaimable("qqpizdazz"));

        options.ProfanityPartialMatching = true;
        AssertCategory(new Checker(options), "qqpizdazz", "profanity");
    }

    /// <summary>Checks that disabling compact matching also disables compact native aliases.</summary>
    [Fact]
    public void NativeAliasesHonorCompactMatchingSwitch()
    {
        var options = MatchingOptions();
        options.AdditionalReserved.Add("zéphyrium");
        options.DisabledRules |= Rule.CompactMatching;
        Assert.True(new Checker(options).IsClaimable("z.e.p.h.y.r.i.u.m"));
        AssertCategory(new Checker(options), "zephyrium", "custom");
    }

    /// <summary>Checks that Unicode aliases do not evade the configured minimum substring length.</summary>
    [Fact]
    public void NativeAliasesHonorPartialLengthThreshold()
    {
        var options = MatchingOptions();
        options.Strictness = Strictness.Strict;
        options.AdditionalReserved.Add("éx");
        Assert.True(new Checker(options).IsClaimable("qqexzz"));
        options.PartialMatchMinimumLength = 2;
        AssertCategory(new Checker(options), "qqexzz", "custom");
    }

    /// <summary>Checks complete language coverage with separator and casing bypass attempts in each category.</summary>
    [Fact]
    public void AllLanguagesRejectAlteredValuesFromAllFourLocalizedCategories()
    {
        var assembly = typeof(Checker).Assembly;
        var codes = new Dictionary<string, Language>
        {
            ["en"] = Language.English, ["nl"] = Language.Dutch, ["de"] = Language.German,
            ["fr"] = Language.French, ["es"] = Language.Spanish, ["it"] = Language.Italian,
            ["pt"] = Language.Portuguese, ["pl"] = Language.Polish, ["tr"] = Language.Turkish,
            ["id"] = Language.Indonesian, ["cs"] = Language.Czech, ["vi"] = Language.Vietnamese,
            ["hu"] = Language.Hungarian, ["sv"] = Language.Swedish, ["ro"] = Language.Romanian
        };
        var covered = new HashSet<(Language, string)>();
        var checkers = new Dictionary<Language, Checker>();
        foreach (var language in codes.Values)
        {
            var options = MatchingOptions();
            options.RemoveLanguage(Language.English);
            options.AddLanguage(language);
            checkers.Add(language, new Checker(options));
        }

        foreach (var resource in assembly.GetManifestResourceNames().OrderBy(value => value))
        {
            if (!resource.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                || !resource.EndsWith(".json", StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var document = System.Text.Json.JsonDocument.Parse(stream);
            var root = document.RootElement;
            var category = root.GetProperty("category").GetString()!;
            if (category == "brands" || category == "technology")
            {
                continue;
            }

            var code = root.TryGetProperty("language", out var codeProperty) ? codeProperty.GetString()! : "en";
            var language = codes[code];
            if (covered.Contains((language, category)))
            {
                continue;
            }

            var checker = checkers[language];
            var value = root.GetProperty("values").EnumerateArray().Select(item => item.GetString()!)
                .First(candidate => checker.Check(candidate).Category == category);
            AssertCategory(checker, string.Join(".", value.Where(char.IsLetterOrDigit)), category);
            AssertCategory(checker, value.Normalize(NormalizationForm.FormD), category);
            covered.Add((language, category));
        }

        Assert.Equal(60, covered.Count);
    }

    /// <summary>Preserves benign names while detecting explicit attempts across strict and relaxed policies.</summary>
    [Theory]
    [InlineData("cocktail")]
    [InlineData("classic")]
    [InlineData("Scunthorpe")]
    [InlineData("Penistone")]
    [InlineData("Cassandra")]
    [InlineData("rapid")]
    [InlineData("Agnieszka")]
    [InlineData("Astrid")]
    public void OrdinaryNamesRemainAllowedWithDefaultProfanityPartialPolicy(string input)
    {
        var options = MatchingOptions();
        options.Strictness = Strictness.Strict;
        Assert.True(new Checker(options).IsClaimable(input), input);
    }

    private static Options MatchingOptions() => new Options
    {
        Strictness = Strictness.Standard,
        DisabledRules = StructuralRules
    };

    private static void AssertCategory(Checker checker, string input, string category)
    {
        var result = checker.Check(input);
        Assert.True(result.IsReserved, $"Bypass in {category}: {input}");
        Assert.Equal(category, result.Category);
        Assert.Equal(result.IsReserved, checker.CheckDetailed(input).IsReserved);
    }
}
