using System.Text;
using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ConfigurationSwitch050Tests
{
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> ExclusiveCategoryValues =
        new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(BuildExclusiveCategoryValues);

    public static IEnumerable<object[]> AllCategories =>
        Enum.GetValues<Category>().Select(category => new object[] { category });

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void EveryCategoryCanBeDisabledAndReenabled(Category category)
    {
        var categoryName = category.ToString().ToLowerInvariant();
        Assert.True(ExclusiveCategoryValues.Value.TryGetValue(categoryName, out var candidates));

        // Standard mode isolates exact category membership from valid cross-category partial matches.
        var options = new Options { Strictness = Strictness.Standard };
        var enabledChecker = new Checker(options);

        options.DisableCategory(category);
        Assert.Contains(category, options.DisabledCategories);
        var disabledChecker = new Checker(options);

        var candidate = candidates!.FirstOrDefault(value =>
        {
            var enabledResult = enabledChecker.Check(value);
            return enabledResult.MatchKind == MatchKind.Exact
                   && enabledResult.Category == categoryName
                   && disabledChecker.IsClaimable(value);
        });

        Assert.False(
            string.IsNullOrEmpty(candidate),
            $"Disabling '{categoryName}' did not make an exclusive exact value claimable in Standard mode.");

        var enabledResult = enabledChecker.Check(candidate);
        Assert.True(enabledResult.IsReserved);
        Assert.Equal(MatchKind.Exact, enabledResult.MatchKind);
        Assert.Equal(categoryName, enabledResult.Category);
        Assert.True(disabledChecker.IsClaimable(candidate));

        options.EnableCategory(category);
        Assert.DoesNotContain(category, options.DisabledCategories);

        var restoredResult = new Checker(options).Check(candidate);
        Assert.True(restoredResult.IsReserved);
        Assert.Equal(MatchKind.Exact, restoredResult.MatchKind);
        Assert.Equal(categoryName, restoredResult.Category);

        // Both already-constructed checkers keep the category state they captured.
        Assert.Equal(categoryName, enabledChecker.Check(candidate).Category);
        Assert.True(disabledChecker.IsClaimable(candidate));
    }

    [Theory]
    [InlineData(Rule.MinimumLength)]
    [InlineData(Rule.MaximumLength)]
    [InlineData(Rule.Whitespace)]
    [InlineData(Rule.BlockedCharacters)]
    [InlineData(Rule.LeadingSeparator)]
    [InlineData(Rule.TrailingSeparator)]
    [InlineData(Rule.Numbers)]
    [InlineData(Rule.CompactMatching)]
    [InlineData(Rule.PartialMatching)]
    [InlineData(Rule.Profanity)]
    [InlineData(Rule.ObfuscationMatching)]
    [InlineData(Rule.UnicodeConfusableMatching)]
    public void EveryRuleCanBeDisabledAndReenabled(Rule rule)
    {
        var testCase = CreateRuleCase(rule);
        var options = testCase.Options;

        var enabled = new Checker(options).Check(testCase.Input);
        Assert.True(enabled.IsReserved, $"Rule '{rule}' should reject its enabled case.");
        Assert.Equal(testCase.ExpectedKind, enabled.MatchKind);

        options.DisabledRules |= rule;
        var disabledChecker = new Checker(options);
        Assert.True(disabledChecker.IsClaimable(testCase.Input), $"Rule '{rule}' remained active after disable.");

        options.DisabledRules &= ~rule;
        var restored = new Checker(options).Check(testCase.Input);
        Assert.True(restored.IsReserved, $"Rule '{rule}' was not restored after re-enable.");
        Assert.Equal(testCase.ExpectedKind, restored.MatchKind);

        Assert.True(disabledChecker.IsClaimable(testCase.Input));
    }

    [Theory]
    [InlineData(ToggleOption.CompactMatching)]
    [InlineData(ToggleOption.PartialMatching)]
    [InlineData(ToggleOption.ProfanityMatching)]
    [InlineData(ToggleOption.ObfuscationMatching)]
    [InlineData(ToggleOption.UnicodeConfusableMatching)]
    [InlineData(ToggleOption.NumberRestriction)]
    [InlineData(ToggleOption.InvisibleOnlyProtection)]
    [InlineData(ToggleOption.ControlCharacterProtection)]
    [InlineData(ToggleOption.FormatCharacterProtection)]
    public void BooleanProtectionOptionsCanBeDisabledAndReenabled(ToggleOption toggle)
    {
        var testCase = CreateToggleCase(toggle);
        var options = testCase.Options;

        var enabled = new Checker(options).Check(testCase.Input);
        Assert.True(enabled.IsReserved, $"Option '{toggle}' should reject its enabled case.");
        Assert.Equal(testCase.ExpectedKind, enabled.MatchKind);

        testCase.Disable(options);
        var disabledChecker = new Checker(options);
        Assert.True(disabledChecker.IsClaimable(testCase.Input), $"Option '{toggle}' remained active after disable.");

        testCase.Enable(options);
        var restored = new Checker(options).Check(testCase.Input);
        Assert.True(restored.IsReserved, $"Option '{toggle}' was not restored after re-enable.");
        Assert.Equal(testCase.ExpectedKind, restored.MatchKind);

        Assert.True(disabledChecker.IsClaimable(testCase.Input));
    }

    [Fact]
    public void AsciiOnlyCanBeEnabledAndDisabled()
    {
        var options = new Options();
        const string value = "qzxé";

        Assert.True(new Checker(options).IsClaimable(value));

        options.AsciiOnly = true;
        var asciiOnly = new Checker(options);
        Assert.Equal(MatchKind.InvalidCharacters, asciiOnly.Check(value).MatchKind);

        options.AsciiOnly = false;
        Assert.True(new Checker(options).IsClaimable(value));
        Assert.Equal(MatchKind.InvalidCharacters, asciiOnly.Check(value).MatchKind);
    }

    [Fact]
    public void StrictnessCanBeChangedInBothDirections()
    {
        var options = new Options { Strictness = Strictness.Strict };
        Assert.Equal(MatchKind.Partial, new Checker(options).Check("supportive").MatchKind);

        options.Strictness = Strictness.Standard;
        var standard = new Checker(options);
        Assert.True(standard.IsClaimable("supportive"));

        options.Strictness = Strictness.Strict;
        Assert.Equal(MatchKind.Partial, new Checker(options).Check("supportive").MatchKind);
        Assert.True(standard.IsClaimable("supportive"));
    }

    [Fact]
    public void PartialMatchMinimumLengthCanBeRaisedAndLowered()
    {
        var options = new Options
        {
            Strictness = Strictness.Strict,
            PartialMatchMinimumLength = 5
        };
        options.AdditionalReserved.Add("qzvx");
        const string compound = "aaqzvxbb";

        var highThreshold = new Checker(options);
        Assert.True(highThreshold.IsClaimable(compound));

        options.PartialMatchMinimumLength = 4;
        Assert.Equal(MatchKind.Partial, new Checker(options).Check(compound).MatchKind);

        options.PartialMatchMinimumLength = 5;
        Assert.True(new Checker(options).IsClaimable(compound));
        Assert.True(highThreshold.IsClaimable(compound));
    }

    [Fact]
    public void ProfanityPartialMatchingCanBeEnabledAndDisabled()
    {
        var options = new Options();
        Assert.True(new Checker(options).IsClaimable("cocktail"));

        options.ProfanityPartialMatching = true;
        var enabled = new Checker(options);
        Assert.Equal(MatchKind.Partial, enabled.Check("cocktail").MatchKind);

        options.ProfanityPartialMatching = false;
        Assert.True(new Checker(options).IsClaimable("cocktail"));
        Assert.Equal(MatchKind.Partial, enabled.Check("cocktail").MatchKind);
    }

    [Fact]
    public void LanguageCanBeAddedRemovedAndAddedAgain()
    {
        var options = new Options();
        const string dutchOnlyValue = "facturatiehulp";
        Assert.True(new Checker(options).IsClaimable(dutchOnlyValue));

        options.AddLanguage(Language.Dutch);
        var withDutch = new Checker(options);
        Assert.Equal("support", withDutch.Check(dutchOnlyValue).Category);

        options.RemoveLanguage(Language.Dutch);
        var withoutDutch = new Checker(options);
        Assert.True(withoutDutch.IsClaimable(dutchOnlyValue));

        options.AddLanguage(Language.Dutch);
        Assert.Equal("support", new Checker(options).Check(dutchOnlyValue).Category);
        Assert.Equal("support", withDutch.Check(dutchOnlyValue).Category);
        Assert.True(withoutDutch.IsClaimable(dutchOnlyValue));
    }

    [Fact]
    public void AllowedIdentifierCanBeAddedRemovedAndAddedAgain()
    {
        var options = new Options();
        options.AllowedIdentifiers.Add("supportive");
        var allowed = new Checker(options);
        Assert.True(allowed.IsClaimable("supportive"));

        options.AllowedIdentifiers.Remove("supportive");
        var removed = new Checker(options);
        Assert.True(removed.IsReserved("supportive"));

        options.AllowedIdentifiers.Add("supportive");
        Assert.True(new Checker(options).IsClaimable("supportive"));
        Assert.True(allowed.IsClaimable("supportive"));
        Assert.True(removed.IsReserved("supportive"));
    }

    [Fact]
    public void AdditionalReservationCanBeAddedRemovedAndAddedAgain()
    {
        var options = new Options();
        const string value = "qzxvorn";
        Assert.True(new Checker(options).IsClaimable(value));

        options.AdditionalReserved.Add(value);
        var reserved = new Checker(options);
        Assert.True(reserved.IsReserved(value));

        options.AdditionalReserved.Remove(value);
        var removed = new Checker(options);
        Assert.True(removed.IsClaimable(value));

        options.AdditionalReserved.Add(value);
        Assert.True(new Checker(options).IsReserved(value));
        Assert.True(reserved.IsReserved(value));
        Assert.True(removed.IsClaimable(value));
    }

    [Fact]
    public void AllowedIdentifierDoesNotDisableCompactOrUnicodeConfusableProtection()
    {
        var options = new Options { DisabledRules = Rule.BlockedCharacters };
        options.AllowedIdentifiers.Add("nike");
        options.AllowedIdentifiers.Add("apple");
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("nike"));
        Assert.True(checker.IsReserved("n.i.k.e"));
        Assert.True(checker.IsClaimable("apple"));
        Assert.True(checker.IsReserved("\u0430pple"));
    }

    [Fact]
    public void ExactCustomReservationStaysExactOnly()
    {
        var options = new Options
        {
            DisabledRules = Rule.Numbers | Rule.BlockedCharacters
        };
        options.Reserve("qzxvorn", ReservedMatchMode.Exact);
        options.Reserve("qaq", ReservedMatchMode.Exact);
        var checker = new Checker(options);

        Assert.True(checker.IsReserved("QZXVORN"));
        Assert.True(checker.IsClaimable("qzx-vorn"));
        Assert.True(checker.IsClaimable("qzxv0rn"));
        Assert.True(checker.IsReserved("QAQ"));
        Assert.True(checker.IsClaimable("q\u0430q"));
    }

    private static RuleCase CreateRuleCase(Rule rule)
    {
        switch (rule)
        {
            case Rule.MinimumLength:
                return new RuleCase(new Options(), "qz", MatchKind.TooShort);
            case Rule.MaximumLength:
                return new RuleCase(new Options(), new string('q', 33), MatchKind.TooLong);
            case Rule.Whitespace:
                return new RuleCase(new Options(), "qzx vorn", MatchKind.BlockedCharacter);
            case Rule.BlockedCharacters:
                return new RuleCase(new Options(), "qzx-vorn", MatchKind.BlockedCharacter);
            case Rule.LeadingSeparator:
                return new RuleCase(new Options(), ".qzxvorn", MatchKind.LeadingSeparator);
            case Rule.TrailingSeparator:
                return new RuleCase(new Options(), "qzxvorn.", MatchKind.TrailingSeparator);
            case Rule.Numbers:
                return new RuleCase(new Options(), "qzxvorn2", MatchKind.NumbersNotAllowed);
            case Rule.CompactMatching:
            {
                var options = new Options
                {
                    Strictness = Strictness.Standard,
                    DisabledRules = Rule.BlockedCharacters
                };
                options.AdditionalReserved.Add("qzxvorn");
                return new RuleCase(options, "qzx-vorn", MatchKind.Compact);
            }
            case Rule.PartialMatching:
            {
                var options = new Options();
                options.AdditionalReserved.Add("qzxvorn");
                return new RuleCase(options, "preqzxvornpost", MatchKind.Partial);
            }
            case Rule.Profanity:
                return new RuleCase(new Options { Strictness = Strictness.Standard }, "fuck", MatchKind.Exact);
            case Rule.ObfuscationMatching:
            {
                var options = new Options
                {
                    Strictness = Strictness.Standard,
                    DisabledRules = Rule.Numbers
                };
                options.AdditionalReserved.Add("qzxvorn");
                return new RuleCase(options, "qzxv0rn", MatchKind.Obfuscated);
            }
            case Rule.UnicodeConfusableMatching:
            {
                var options = new Options { Strictness = Strictness.Standard };
                options.AdditionalReserved.Add("qaq");
                return new RuleCase(options, "q\u0430q", MatchKind.UnicodeConfusable);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unsupported rule toggle case.");
        }
    }

    private static ToggleCase CreateToggleCase(ToggleOption toggle)
    {
        switch (toggle)
        {
            case ToggleOption.CompactMatching:
            {
                var options = new Options
                {
                    Strictness = Strictness.Standard,
                    DisabledRules = Rule.BlockedCharacters
                };
                options.AdditionalReserved.Add("qzxvorn");
                return new ToggleCase(options, "qzx-vorn", MatchKind.Compact,
                    value => value.CompactMatching = false,
                    value => value.CompactMatching = true);
            }
            case ToggleOption.PartialMatching:
            {
                var options = new Options
                {
                    Strictness = Strictness.Standard,
                    PartialMatching = true
                };
                options.AdditionalReserved.Add("qzxvorn");
                return new ToggleCase(options, "preqzxvornpost", MatchKind.Partial,
                    value => value.PartialMatching = false,
                    value => value.PartialMatching = true);
            }
            case ToggleOption.ProfanityMatching:
                return new ToggleCase(new Options { Strictness = Strictness.Standard }, "fuck", MatchKind.Exact,
                    value => value.ProfanityMatching = false,
                    value => value.ProfanityMatching = true);
            case ToggleOption.ObfuscationMatching:
            {
                var options = new Options
                {
                    Strictness = Strictness.Standard,
                    DisabledRules = Rule.Numbers
                };
                options.AdditionalReserved.Add("qzxvorn");
                return new ToggleCase(options, "qzxv0rn", MatchKind.Obfuscated,
                    value => value.ObfuscationMatching = false,
                    value => value.ObfuscationMatching = true);
            }
            case ToggleOption.UnicodeConfusableMatching:
            {
                var options = new Options { Strictness = Strictness.Standard };
                options.AdditionalReserved.Add("qaq");
                return new ToggleCase(options, "q\u0430q", MatchKind.UnicodeConfusable,
                    value => value.UnicodeConfusableMatching = false,
                    value => value.UnicodeConfusableMatching = true);
            }
            case ToggleOption.NumberRestriction:
                return new ToggleCase(new Options(), "qzxvorn2", MatchKind.NumbersNotAllowed,
                    value => value.AllowNumbers = true,
                    value => value.AllowNumbers = false);
            case ToggleOption.InvisibleOnlyProtection:
                return new ToggleCase(new Options(), "\u0301\u0301\u0301", MatchKind.InvalidCharacters,
                    value => value.RejectInvisibleOnlyIdentifiers = false,
                    value => value.RejectInvisibleOnlyIdentifiers = true);
            case ToggleOption.ControlCharacterProtection:
                return new ToggleCase(new Options(), "qz\u0001vx", MatchKind.InvalidCharacters,
                    value => value.RejectControlCharacters = false,
                    value => value.RejectControlCharacters = true);
            case ToggleOption.FormatCharacterProtection:
                return new ToggleCase(new Options(), "qz\u200Dvx", MatchKind.InvalidCharacters,
                    value => value.RejectFormatCharacters = false,
                    value => value.RejectFormatCharacters = true);
            default:
                throw new ArgumentOutOfRangeException(nameof(toggle), toggle, "Unsupported option toggle case.");
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildExclusiveCategoryValues()
    {
        var categoriesByValue = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var originalByNormalized = new Dictionary<string, string>(StringComparer.Ordinal);
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
            if (!document.RootElement.TryGetProperty("category", out var categoryElement))
            {
                continue;
            }

            var category = categoryElement.GetString();
            if (string.IsNullOrWhiteSpace(category)
                || !Enum.TryParse<Category>(category, ignoreCase: true, out _))
            {
                continue;
            }

            foreach (var value in EnumerateDatasetValues(document.RootElement))
            {
                if (!IsStructurallySafeCategoryCandidate(value))
                {
                    continue;
                }

                var normalized = value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
                if (!categoriesByValue.TryGetValue(normalized, out var categories))
                {
                    categories = new HashSet<string>(StringComparer.Ordinal);
                    categoriesByValue.Add(normalized, categories);
                    originalByNormalized.Add(normalized, value);
                }

                categories.Add(category);
            }
        }

        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var pair in categoriesByValue)
        {
            if (pair.Value.Count != 1)
            {
                continue;
            }

            var category = pair.Value.Single();
            if (!result.TryGetValue(category, out var values))
            {
                values = new List<string>();
                result.Add(category, values);
            }

            values.Add(originalByNormalized[pair.Key]);
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            StringComparer.Ordinal);
    }

    private static IEnumerable<string> EnumerateDatasetValues(JsonElement rootElement)
    {
        if (rootElement.TryGetProperty("values", out var values))
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

        if (rootElement.TryGetProperty("partialValues", out var partialValues))
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

        if (!rootElement.TryGetProperty("combinations", out var combinations))
        {
            yield break;
        }

        foreach (var combination in combinations.EnumerateArray())
        {
            if (!combination.TryGetProperty("roots", out var roots)
                || !combination.TryGetProperty("suffixes", out var suffixes))
            {
                continue;
            }

            var suffixValues = suffixes.EnumerateArray()
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

    private static bool IsStructurallySafeCategoryCandidate(string value) =>
        value.Length >= 3
        && value.Length <= 32
        && value.All(character =>
            (character >= 'a' && character <= 'z')
            || (character >= 'A' && character <= 'Z'));

    public enum ToggleOption
    {
        CompactMatching,
        PartialMatching,
        ProfanityMatching,
        ObfuscationMatching,
        UnicodeConfusableMatching,
        NumberRestriction,
        InvisibleOnlyProtection,
        ControlCharacterProtection,
        FormatCharacterProtection
    }

    private sealed class RuleCase
    {
        public RuleCase(Options options, string input, MatchKind expectedKind)
        {
            Options = options;
            Input = input;
            ExpectedKind = expectedKind;
        }

        public Options Options { get; }
        public string Input { get; }
        public MatchKind ExpectedKind { get; }
    }

    private sealed class ToggleCase
    {
        public ToggleCase(Options options, string input, MatchKind expectedKind, Action<Options> disable, Action<Options> enable)
        {
            Options = options;
            Input = input;
            ExpectedKind = expectedKind;
            Disable = disable;
            Enable = enable;
        }

        public Options Options { get; }
        public string Input { get; }
        public MatchKind ExpectedKind { get; }
        public Action<Options> Disable { get; }
        public Action<Options> Enable { get; }
    }
}
