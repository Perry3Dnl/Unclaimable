using System.Text;
using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ConfigurationSwitch050Tests
{
    private static readonly Checker DefaultChecker = new Checker();
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> ExclusiveCategoryValues =
        new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(BuildExclusiveCategoryValues);

    public static IEnumerable<object[]> AllCategories =>
        Enum.GetValues<Category>().Select(category => new object[] { category });

    [Theory]
    [MemberData(nameof(AllCategories))]
    public void EveryCategoryCanBeDisabledAndReenabled(Category category)
    {
        var categoryName = category.ToString().ToLowerInvariant();
        Assert.True(
            ExclusiveCategoryValues.Value.TryGetValue(categoryName, out var candidates),
            $"No exclusive test candidates were found for category '{categoryName}'.");

        var options = new Options();
        options.DisableCategory(category);
        Assert.Contains(category, options.DisabledCategories);

        var disabledChecker = new Checker(options);
        var candidate = candidates!
            .FirstOrDefault(value =>
            {
                var defaultResult = DefaultChecker.Check(value);
                return defaultResult.MatchKind == MatchKind.Exact
                       && string.Equals(defaultResult.Category, categoryName, StringComparison.Ordinal)
                       && disabledChecker.IsClaimable(value);
            });

        Assert.False(
            string.IsNullOrEmpty(candidate),
            $"Disabling category '{categoryName}' did not make any exclusive exact value claimable.");

        var enabledResult = DefaultChecker.Check(candidate);
        Assert.True(enabledResult.IsReserved);
        Assert.Equal(MatchKind.Exact, enabledResult.MatchKind);
        Assert.Equal(categoryName, enabledResult.Category);
        Assert.True(disabledChecker.IsClaimable(candidate));

        options.EnableCategory(category);
        Assert.DoesNotContain(category, options.DisabledCategories);

        var reenabledChecker = new Checker(options);
        var reenabledResult = reenabledChecker.Check(candidate);
        Assert.True(reenabledResult.IsReserved);
        Assert.Equal(MatchKind.Exact, reenabledResult.MatchKind);
        Assert.Equal(categoryName, reenabledResult.Category);

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

        var enabledChecker = new Checker(options);
        var enabledResult = enabledChecker.Check(testCase.Input);
        Assert.True(enabledResult.IsReserved, $"Rule '{rule}' should reject the enabled test case.");
        Assert.Equal(testCase.ExpectedKind, enabledResult.MatchKind);

        options.DisabledRules |= rule;
        var disabledChecker = new Checker(options);
        Assert.True(
            disabledChecker.IsClaimable(testCase.Input),
            $"Rule '{rule}' remained effective after being disabled.");

        options.DisabledRules &= ~rule;
        var reenabledChecker = new Checker(options);
        var reenabledResult = reenabledChecker.Check(testCase.Input);
        Assert.True(reenabledResult.IsReserved, $"Rule '{rule}' was not restored after being re-enabled.");
        Assert.Equal(testCase.ExpectedKind, reenabledResult.MatchKind);

        Assert.True(disabledChecker.IsClaimable(testCase.Input));
    }

    [Fact]
    public void CompactMatchingBooleanCanBeDisabledAndReenabled()
    {
        var options = new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = Rule.BlockedCharacters
        };
        options.AdditionalReserved.Add("qzxvorn");

        Assert.Equal(MatchKind.Compact, new Checker(options).Check("qzx-vorn").MatchKind);

        options.CompactMatching = false;
        var disabled = new Checker(options);
        Assert.True(disabled.IsClaimable("qzx-vorn"));

        options.CompactMatching = true;
        Assert.Equal(MatchKind.Compact, new Checker(options).Check("qzx-vorn").MatchKind);
        Assert.True(disabled.IsClaimable("qzx-vorn"));
    }

    [Fact]
    public void PartialMatchingBooleanCanBeDisabledAndReenabledInStandardMode()
    {
        var options = new Options
        {
            Strictness = Strictness.Standard,
            PartialMatching = true
        };
        options.AdditionalReserved.Add("qzxvorn");

        Assert.Equal(MatchKind.Partial, new Checker(options).Check("preqzxvornpost").MatchKind);

        options.PartialMatching = false;
        var disabled = new Checker(options);
        Assert.True(disabled.IsClaimable("preqzxvornpost"));

        options.PartialMatching = true;
        Assert.Equal(MatchKind.Partial, new Checker(options).Check("preqzxvornpost").MatchKind);
        Assert.True(disabled.IsClaimable("preqzxvornpost"));
    }

    [Fact]
    public void ProfanityMatchingBooleanCanBeDisabledAndReenabled()
    {
        var options = new Options { Strictness = Strictness.Standard };

        Assert.Equal("profanity", new Checker(options).Check("fuck").Category);

        options.ProfanityMatching = false;
        var disabled = new Checker(options);
        Assert.True(disabled.IsClaimable("fuck"));

        options.ProfanityMatching = true;
        Assert.Equal("profanity", new Checker(options).Check("fuck").Category);
        Assert.True(disabled.IsClaimable("fuck"));
    }

    [Fact]
    public void ObfuscationMatchingBooleanCanBeDisabledAndReenabled()
    {
        var options = new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = Rule.Numbers
        };
        options.AdditionalReserved.Add("qzxvorn");

        Assert.Equal(MatchKind.Obfuscated, new Checker(options).Check("qzxv0rn").MatchKind);

        options.ObfuscationMatching = false;
        var disabled = new Checker(options);
        Assert.True(disabled.IsClaimable("qzxv0rn"));

        options.ObfuscationMatching = true;
        Assert.Equal(MatchKind.Obfuscated, new Checker(options).Check("qzxv0rn").MatchKind);
        Assert.True(disabled.IsClaimable("qzxv0rn"));
    }

    [Fact]
    public void UnicodeConfusableMatchingBooleanCanBeDisabledAndReenabled()
    {
        var options = new Options { Strictness = Strictness.Standard };
        options.AdditionalReserved.Add("qaq");
        const string confusable = "q\u0430q";

        Assert.Equal(MatchKind.UnicodeConfusable, new Checker(options).Check(confusable).MatchKind);

        options.UnicodeConfusableMatching = false;
        var disabled = new Checker(options);
        Assert.True(disabled.IsClaimable(confusable));

        options.UnicodeConfusableMatching = true;
        Assert.Equal(MatchKind.UnicodeConfusable, new Checker(options).Check(confusable).MatchKind);
        Assert.True(disabled.IsClaimable(confusable));
    }

    [Fact]
    public void NumberOptionCanBeEnabledAndDisabled()
    {
        var options = new Options();
        const string value = "qzxvorn2";

        Assert.Equal(MatchKind.NumbersNotAllowed, new Checker(options).Check(value).MatchKind);

        options.AllowNumbers = true;
        var allowed = new Checker(options);
        Assert.True(allowed.IsClaimable(value));

        options.AllowNumbers = false;
        Assert.Equal(MatchKind.NumbersNotAllowed, new Checker(options).Check(value).MatchKind);
        Assert.True(allowed.IsClaimable(value));
    }

    [Fact]
    public void AsciiOnlyOptionCanBeEnabledAndDisabled()
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
    public void InvisibleOnlyProtectionCanBeDisabledAndReenabled()
    {
        var options = new Options();
        const string value = "\u0301\u0301\u0301";

        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(value).MatchKind);

        options.RejectInvisibleOnlyIdentifiers = false;
        var relaxed = new Checker(options);
        Assert.True(relaxed.IsClaimable(value));

        options.RejectInvisibleOnlyIdentifiers = true;
        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(value).MatchKind);
        Assert.True(relaxed.IsClaimable(value));
    }

    [Fact]
    public void ControlCharacterProtectionCanBeDisabledAndReenabled()
    {
        var options = new Options();
        const string value = "qz\u0001vx";

        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(value).MatchKind);

        options.RejectControlCharacters = false;
        var relaxed = new Checker(options);
        Assert.True(relaxed.IsClaimable(value));

        options.RejectControlCharacters = true;
        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(value).MatchKind);
        Assert.True(relaxed.IsClaimable(value));
    }

    [Fact]
    public void FormatCharacterProtectionCanBeDisabledAndReenabled()
    {
        var options = new Options();
        const string value = "qz\u200Dvx";

        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(value).MatchKind);

        options.RejectFormatCharacters = false;
        var relaxed = new Checker(options);
        Assert.True(relaxed.IsClaimable(value));

        options.RejectFormatCharacters = true;
        Assert.Equal(MatchKind.InvalidCharacters, new Checker(options).Check(value).MatchKind);
        Assert.True(relaxed.IsClaimable(value));
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
        var options = new Options
        {
            DisabledRules = Rule.BlockedCharacters
        };
        options.AllowedIdentifiers.Add("nike");
        options.AllowedIdentifiers.Add("apple");

        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("nike"));
        Assert.True(checker.IsReserved("n.i.k.e"));
        Assert.True(checker.IsClaimable("apple"));
        Assert.True(checker.IsReserved("\u0430pple"));
    }

    [Fact]
    public void ExactCustomReservationDoesNotEnableCompactObfuscationOrUnicodeConfusableMatching()
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

    private static IEnumerable<string> EnumerateDatasetValues(JsonElement root)
    {
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

            foreach (var root in roots.EnumerateArray())
            {
                var rootText = root.GetString();
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
}
