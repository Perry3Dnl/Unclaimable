using System.Text;
using System.Reflection;
using System.Text.Json;
using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class Extended076Tests
{
    [Fact]
    public void SharedVersionAddsWholeIdentifierWithoutChangingExistingEnumValues()
    {
        Assert.Equal(0, (int)ReservedMatchMode.Default);
        Assert.Equal(1, (int)ReservedMatchMode.Exact);
        Assert.Equal(2, (int)ReservedMatchMode.WholeIdentifier);
    }

    [Fact]
    public void CategorizedWholeIdentifierUsesCoreMatchingWithoutBecomingPartial()
    {
        var options = new Options();
        options.Reserve("Example Identity", "extendedtest", ReservedMatchMode.WholeIdentifier);
        var checker = new Checker(options);

        var compact = checker.Check("ExampleIdentity");
        Assert.True(compact.IsReserved);
        Assert.Equal("extendedtest", compact.Category);
        Assert.Equal(MatchKind.Compact, compact.MatchKind);

        var obfuscated = checker.Check("3xampleIdentity");
        Assert.True(obfuscated.IsReserved);
        Assert.Equal("extendedtest", obfuscated.Category);
        Assert.Equal(MatchKind.Obfuscated, obfuscated.MatchKind);

        var confusable = checker.Check("\u0435xampleidentity");
        Assert.True(confusable.IsReserved);
        Assert.Equal("extendedtest", confusable.Category);
        Assert.Equal(MatchKind.UnicodeConfusable, confusable.MatchKind);

        Assert.True(checker.IsClaimable("myexampleidentityfan"));
    }

    [Fact]
    public void CategorizedReservationValidatesCategory()
    {
        var options = new Options();

        Assert.Throws<ArgumentException>(
            () => options.Reserve("exampleidentity", " ", ReservedMatchMode.WholeIdentifier));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => options.Reserve("exampleidentity", "test", (ReservedMatchMode)999));
    }

    [Fact]
    public void MerelyReferencingExtendedDoesNotChangeCoreBehavior()
    {
        Assert.True(new Checker().IsClaimable("aalborguniversity"));
    }

    [Fact]
    public void UseExtendedDataActivatesLargeSnapshotThroughCoreChecker()
    {
        var options = new Options();
        options.UseExtendedData();
        var checker = new Checker(options);

        AssertCategory(checker, "1stdibscom", "companies");
        AssertCategory(checker, "9round", "regionalbrands");
        AssertCategory(checker, "aalborguniversity", "education");
        AssertCategory(checker, "aalborgairport", "transport");
        AssertCategory(checker, "kashimaantlers", "sports");
        AssertCategory(checker, "raiffeisenbankinternational", "financialinstitutions");
        AssertCategory(checker, "rijkswaterstaat", "government");
        AssertCategory(checker, "worldmeteorologicalorganization", "internationalorganizations");
        AssertCategory(checker, "süddeutschezeitung", "media");
        AssertCategory(checker, "boehringeringelheim", "healthcare");
        AssertCategory(checker, "johanneskepler", "historicalfigures");
        AssertCategory(checker, "friedrichmerz", "publicfigures");
        AssertCategory(checker, "sabrinacarpenter", "celebrities");
        AssertCategory(checker, "severussnape", "fiction");
        AssertCategory(checker, "reddeadredemption", "entertainment");
        AssertCategory(checker, "airtrafficcontroller", "professions");
        AssertCategory(checker, "officieelaccount", "multilingual");
        AssertCategory(checker, "fuckboi", "slangprofanity");
        AssertCategory(checker, "celestia", "crypto");
        AssertCategory(checker, "kakaotalk", "platforms");
        AssertCategory(checker, "aargau", "geography");
    }

    [Fact]
    public void ExtendedCategoriesCanBeDisabledIndependently()
    {
        var options = new Options();
        options.UseExtendedData(extended => extended.DisableCategory(ExtendedCategory.Education));
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("aalborguniversity"));
        Assert.True(checker.IsReserved("aalborgairport"));

        options = new Options();
        options.UseExtendedData(extended =>
        {
            extended.DisableCategory(ExtendedCategory.Education);
            extended.EnableCategory(ExtendedCategory.Education);
        });

        Assert.True(new Checker(options).IsReserved("aalborguniversity"));
    }

    [Fact]
    public void ExtendedExactExceptionsAreNarrow()
    {
        var options = new Options();
        options.UseExtendedData(extended =>
            extended.AllowedIdentifiers.Add("Aalborg University"));
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("aalborguniversity"));
        Assert.True(checker.IsReserved("aalborgairport"));
    }

    [Fact]
    public void InvalidExtendedConfigurationIsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => ExtendedOptionsExtensions.UseExtendedData(null!));

        var extended = new ExtendedOptions();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => extended.DisableCategory((ExtendedCategory)999));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => extended.EnableCategory((ExtendedCategory)999));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ExtendedData.GetCount((ExtendedCategory)999));
    }

    [Fact]
    public void SnapshotHasExpectedBreadth()
    {
        Assert.True(ExtendedData.TotalEntries >= 36000);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.Companies) >= 12000);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.RegionalBrands) >= 600);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.FinancialInstitutions) >= 1000);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.Sports) >= 2700);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.Education) >= 10000);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.Transport) >= 3400);
        Assert.True(ExtendedData.GetCount(ExtendedCategory.Geography) >= 3700);

        foreach (var category in Enum.GetValues<ExtendedCategory>())
        {
            Assert.True(
                ExtendedData.GetCount(category) >= 50,
                $"Expected at least 50 entries for {category}.");
        }
    }

    [Fact]
    public void EmbeddedDatasetsHaveNoExactOrCompactDuplicates()
    {
        var exact = new HashSet<string>(StringComparer.Ordinal);
        var compact = new HashSet<string>(StringComparer.Ordinal);
        var assembly = typeof(ExtendedData).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("Unclaimable.Extended.Data.", StringComparison.Ordinal)
                           && name.EndsWith(".json", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(Enum.GetValues<ExtendedCategory>().Length, resourceNames.Length);

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);
            using var document = JsonDocument.Parse(stream!);
            var root = document.RootElement;

            Assert.Equal(1, root.GetProperty("schema").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("category").GetString()));

            foreach (var item in root.GetProperty("values").EnumerateArray())
            {
                var value = item.GetString();
                Assert.False(string.IsNullOrWhiteSpace(value));

                var normalized = value!.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
                var compactValue = new string(normalized.Where(char.IsLetterOrDigit).ToArray());

                Assert.True(exact.Add(normalized), $"Duplicate Extended value: {value}");
                Assert.True(compact.Add(compactValue), $"Duplicate compact Extended value: {value}");
            }
        }
    }

    [Fact]
    public void ImportedArtifactMarkersAreNotPresent()
    {
        var options = new Options();
        options.UseExtendedData();
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable("duplicateyeg"));
        Assert.True(checker.IsClaimable("unassignedcounty"));
        Assert.True(checker.IsClaimable("offshoreoilplatforms"));
    }

    private static void AssertCategory(Checker checker, string value, string expectedCategory)
    {
        var result = checker.Check(value);
        Assert.True(result.IsReserved, $"Expected '{value}' to be reserved.");
        Assert.Equal(expectedCategory, result.Category);
    }
}
