using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ExtendedData075Tests
{
    private sealed class Document
    {
        [JsonPropertyName("schema")]
        public int Schema { get; init; }

        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        [JsonPropertyName("values")]
        public string[] Values { get; init; } = Array.Empty<string>();
    }

    private static readonly Lazy<IReadOnlyList<(string Category, string Value)>> Entries =
        new Lazy<IReadOnlyList<(string Category, string Value)>>(LoadEntries);

    [Fact]
    public void InstallingExtendedPackageDoesNotActivateItsData()
    {
        var coreOnly = new Checker(CreateRelaxedOptions());

        Assert.True(coreOnly.IsClaimable("harvarduniversity"));
        Assert.True(coreOnly.IsClaimable("cillianmurphy"));
        Assert.True(coreOnly.IsClaimable("massgeneralbrigham"));
    }

    [Fact]
    public void UseExtendedDataActivatesAllDatasetGroups()
    {
        var options = CreateRelaxedOptions();
        options.UseExtendedData();
        var checker = new Checker(options);

        var categories = Entries.Value
            .Select(entry => entry.Category)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(21, categories.Length);

        foreach (var category in categories)
        {
            var candidate = FindExtendedOnlyCandidate(category);
            var result = checker.Check(candidate.Value);

            Assert.True(result.IsReserved, $"{category}: {candidate.Value}");
            Assert.Equal(category, result.Category);
        }
    }

    [Theory]
    [InlineData(ExtendedCategory.CompaniesAndBrands)]
    [InlineData(ExtendedCategory.RegionalBrands)]
    [InlineData(ExtendedCategory.FinancialInstitutions)]
    [InlineData(ExtendedCategory.GovernmentBodies)]
    [InlineData(ExtendedCategory.InternationalOrganizations)]
    [InlineData(ExtendedCategory.Sports)]
    [InlineData(ExtendedCategory.Education)]
    [InlineData(ExtendedCategory.Media)]
    [InlineData(ExtendedCategory.Transport)]
    [InlineData(ExtendedCategory.Healthcare)]
    [InlineData(ExtendedCategory.HistoricalFigures)]
    [InlineData(ExtendedCategory.PublicFigures)]
    [InlineData(ExtendedCategory.Celebrities)]
    [InlineData(ExtendedCategory.Fiction)]
    [InlineData(ExtendedCategory.Entertainment)]
    [InlineData(ExtendedCategory.Professions)]
    [InlineData(ExtendedCategory.MultilingualReserved)]
    [InlineData(ExtendedCategory.RegionalProfanity)]
    [InlineData(ExtendedCategory.Crypto)]
    [InlineData(ExtendedCategory.WebServices)]
    [InlineData(ExtendedCategory.Geography)]
    public void EveryExtendedCategoryCanBeDisabledIndependently(ExtendedCategory category)
    {
        var categoryName = GetCategoryName(category);
        var candidate = FindExtendedOnlyCandidate(categoryName);

        var enabledOptions = CreateRelaxedOptions();
        enabledOptions.UseExtendedData();
        Assert.True(new Checker(enabledOptions).IsReserved(candidate.Value));

        var disabledOptions = CreateRelaxedOptions();
        disabledOptions.UseExtendedData(extended => extended.DisableCategory(category));

        Assert.True(
            new Checker(disabledOptions).IsClaimable(candidate.Value),
            $"{category} should be disabled for {candidate.Value}.");
    }

    [Fact]
    public void ExtendedCategoryCanBeReenabled()
    {
        var candidate = FindExtendedOnlyCandidate("education");
        var options = CreateRelaxedOptions();

        options.UseExtendedData(extended =>
        {
            extended
                .DisableCategory(ExtendedCategory.Education)
                .EnableCategory(ExtendedCategory.Education);
        });

        Assert.Equal("education", new Checker(options).Check(candidate.Value).Category);
    }

    [Fact]
    public void ExtendedUsesWholeIdentifierMatchingWithoutGenericSubstringExpansion()
    {
        var options = CreateRelaxedOptions();
        options.UseExtendedData();
        var checker = new Checker(options);

        var exact = checker.Check("harvard university");
        Assert.True(exact.IsReserved);
        Assert.Equal("education", exact.Category);

        var compact = checker.Check("harvard-university");
        Assert.True(compact.IsReserved);
        Assert.Equal(MatchKind.Compact, compact.MatchKind);

        var confusable = checker.Check("hаrvarduniversity"); // Cyrillic a.
        Assert.True(confusable.IsReserved);
        Assert.Equal(MatchKind.UnicodeConfusable, confusable.MatchKind);

        var obfuscated = checker.Check("h4rvarduniversity");
        Assert.True(obfuscated.IsReserved);
        Assert.Equal(MatchKind.Obfuscated, obfuscated.MatchKind);

        Assert.True(checker.IsClaimable("myharvarduniversityfan"));
    }

    [Fact]
    public void EveryShippedExtendedValueIsIndexedByCore()
    {
        var options = CreateRelaxedOptions();
        options.UseExtendedData();
        var checker = new Checker(options);

        foreach (var entry in Entries.Value)
        {
            var result = checker.Check(entry.Value);

            Assert.True(
                result.IsReserved,
                $"Extended value '{entry.Value}' from '{entry.Category}' was not indexed.");
        }
    }

    [Fact]
    public void ExtendedDatasetIsLargeAndEveryCategoryHasData()
    {
        Assert.True(
            Entries.Value.Count >= 27000,
            $"Expected at least 27,000 Extended entries, found {Entries.Value.Count}.");

        foreach (var category in Enum.GetValues<ExtendedCategory>())
        {
            var name = GetCategoryName(category);
            Assert.Contains(Entries.Value, entry => entry.Category == name);
        }
    }

    [Fact]
    public void ExtendedResourcesHaveValidShapeAndCleanValues()
    {
        var assembly = typeof(ExtendedOptionsExtensions).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("Unclaimable.Extended.Data.", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.True(resourceNames.Length >= 21);

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);

            var document = JsonSerializer.Deserialize<Document>(stream!);
            Assert.NotNull(document);
            Assert.Equal(1, document!.Schema);
            Assert.False(string.IsNullOrWhiteSpace(document.Category));
            Assert.NotEmpty(document.Values);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in document.Values)
            {
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.Equal(value.Trim(), value);
                Assert.Equal(value.ToLowerInvariant(), value);
                Assert.True(seen.Add(value), $"Duplicate '{value}' in {resourceName}.");
            }
        }
    }

    [Fact]
    public void InvalidExtendedCategoryIsRejected()
    {
        var extended = new ExtendedOptions();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => extended.DisableCategory((ExtendedCategory)999));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => extended.EnableCategory((ExtendedCategory)999));
    }

    [Fact]
    public void UseExtendedDataRejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(
            () => ExtendedOptionsExtensions.UseExtendedData(null!));
    }

    private static IReadOnlyList<(string Category, string Value)> LoadEntries()
    {
        var assembly = typeof(ExtendedOptionsExtensions).Assembly;
        var entries = new List<(string Category, string Value)>();

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Extended.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                throw new InvalidOperationException(resourceName);
            }

            var document = JsonSerializer.Deserialize<Document>(stream)
                           ?? throw new InvalidOperationException(resourceName);

            entries.AddRange(document.Values.Select(value => (document.Category, value)));
        }

        return entries;
    }

    private static (string Category, string Value) FindExtendedOnlyCandidate(string category)
    {
        var ownership = Entries.Value
            .GroupBy(entry => entry.Value, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.Category).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        var core = new Checker(CreateRelaxedOptions());

        foreach (var entry in Entries.Value.Where(entry => entry.Category == category))
        {
            if (entry.Value.Length > 48)
            {
                continue;
            }

            if (ownership[entry.Value].Length != 1)
            {
                continue;
            }

            if (core.IsClaimable(entry.Value))
            {
                return entry;
            }
        }

        throw new InvalidOperationException($"No Extended-only candidate found for category '{category}'.");
    }

    private static Options CreateRelaxedOptions()
    {
        var options = new Options
        {
            MinimumLength = 0,
            MaximumLength = 128
        };

        options.DisableRule(
            Rule.Whitespace
            | Rule.BlockedCharacters
            | Rule.LeadingSeparator
            | Rule.TrailingSeparator);
        options.DisablePattern(
            Pattern.NumericOnly
            | Pattern.Repeated
            | Pattern.SymbolOnly
            | Pattern.AsciiArt
            | Pattern.UppercaseOnly);

        return options;
    }

    private static string GetCategoryName(ExtendedCategory category)
    {
        return category switch
        {
            ExtendedCategory.CompaniesAndBrands => "brands",
            ExtendedCategory.RegionalBrands => "regionalbrands",
            ExtendedCategory.FinancialInstitutions => "financial",
            ExtendedCategory.GovernmentBodies => "government",
            ExtendedCategory.InternationalOrganizations => "internationalorganizations",
            ExtendedCategory.Sports => "sports",
            ExtendedCategory.Education => "education",
            ExtendedCategory.Media => "media",
            ExtendedCategory.Transport => "transport",
            ExtendedCategory.Healthcare => "healthcare",
            ExtendedCategory.HistoricalFigures => "historicalfigures",
            ExtendedCategory.PublicFigures => "publicfigures",
            ExtendedCategory.Celebrities => "celebrities",
            ExtendedCategory.Fiction => "fiction",
            ExtendedCategory.Entertainment => "entertainment",
            ExtendedCategory.Professions => "professions",
            ExtendedCategory.MultilingualReserved => "multilingual",
            ExtendedCategory.RegionalProfanity => "regionalprofanity",
            ExtendedCategory.Crypto => "crypto",
            ExtendedCategory.WebServices => "webservices",
            ExtendedCategory.Geography => "geography",
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }
}
