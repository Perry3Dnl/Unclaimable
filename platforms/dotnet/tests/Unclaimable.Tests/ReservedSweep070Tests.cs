using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ReservedSweep070Tests
{
    private const Rule StructuralRules = Rule.Numbers
        | Rule.BlockedCharacters
        | Rule.Whitespace
        | Rule.LeadingSeparator
        | Rule.TrailingSeparator;

    private static readonly Checker DatasetChecker = new Checker(new Options
    {
        Strictness = Strictness.Standard,
        DisabledRules = StructuralRules
    });

    private static readonly HashSet<string> NewGlobalCoreCategories = new(StringComparer.Ordinal)
    {
        "authentication",
        "communications",
        "developer",
        "finance",
        "governance",
        "identity",
        "infrastructure",
        "moderation",
        "official",
        "operations"
    };

    private static readonly Dictionary<Category, string[]> ExistingCoreSweepValues = new()
    {
        [Category.Automation] = new[]
        {
            "automationaccount", "automationservice", "botaccount", "botservice",
            "eventautomation", "eventrunner", "jobautomation", "processautomation",
            "scheduledjob", "scheduledtask", "serviceautomation", "taskautomation",
            "unattendedautomation", "workflowadmin", "workflowservice", "workflowworker"
        },
        [Category.Commerce] = new[]
        {
            "buyer", "buyers", "cart", "carts", "catalog", "catalogue", "customer",
            "customers", "deliveries", "delivery", "inventory", "marketplace", "order",
            "orders", "product", "products", "return", "returns", "shipment", "shipments",
            "shipping", "shop", "stock", "vendor", "vendors"
        },
        [Category.Legal] = new[]
        {
            "datarequest", "datarequests", "dsar", "eula", "lawfulrequest", "lawfulrequests",
            "legalclaims", "legalcomplaint", "legalcomplaints", "legaldisclosure", "legalhold",
            "legalholds", "legalprocess", "license", "licenses", "licensing", "litigation",
            "privacyrights", "subpoenas", "takedown", "takedowns", "terms", "tos",
            "trademarkclaim", "trademarkclaims"
        },
        [Category.Security] = new[]
        {
            "abuseprevention", "antifraud", "antiphishing", "breach", "breaches", "exploit",
            "exploits", "frauddetection", "malware", "phishing", "securityalert",
            "securityalerts", "securityincident", "securityincidents", "securitymonitoring",
            "threat", "threatmonitoring", "threats", "vulnerabilityreport", "vulnerabilityreports"
        }
    };

    [Theory]
    [InlineData("authentication", "authentication")]
    [InlineData("automationservice", "automation")]
    [InlineData("announcement", "communications")]
    [InlineData("member", "community")]
    [InlineData("membership", "community")]
    [InlineData("buyer", "commerce")]
    [InlineData("apikey", "developer")]
    [InlineData("treasury", "finance")]
    [InlineData("vote", "governance")]
    [InlineData("election", "governance")]
    [InlineData("username", "identity")]
    [InlineData("loadbalancer", "infrastructure")]
    [InlineData("legalhold", "legal")]
    [InlineData("banned", "moderation")]
    [InlineData("verified", "official")]
    [InlineData("operations", "operations")]
    [InlineData("phishing", "security")]
    [InlineData("active", "system")]
    [InlineData("pending", "system")]
    public void NewStandaloneNamesAreReservedInExpectedCategory(string value, string category)
    {
        var result = DatasetChecker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(category, result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
    }

    [Theory]
    [InlineData("rememberme")]
    [InlineData("devote")]
    [InlineData("hyperactive")]
    [InlineData("memberlane")]
    [InlineData("voteworthy")]
    public void ExactSweepValuesDoNotBecomeGenericSubstringRules(string value)
    {
        Assert.True(DatasetChecker.IsClaimable(value), value);
    }

    [Theory]
    [InlineData(Category.Authentication, "authentication")]
    [InlineData(Category.Automation, "automationservice")]
    [InlineData(Category.Communications, "announcement")]
    [InlineData(Category.Community, "member")]
    [InlineData(Category.Commerce, "buyer")]
    [InlineData(Category.Developer, "apikey")]
    [InlineData(Category.Finance, "treasury")]
    [InlineData(Category.Governance, "vote")]
    [InlineData(Category.Identity, "username")]
    [InlineData(Category.Infrastructure, "loadbalancer")]
    [InlineData(Category.Legal, "legalhold")]
    [InlineData(Category.Moderation, "banned")]
    [InlineData(Category.Official, "verified")]
    [InlineData(Category.Operations, "operations")]
    [InlineData(Category.Security, "phishing")]
    [InlineData(Category.System, "active")]
    public void NewSweepValuesRespectCategoryDisable(Category category, string value)
    {
        var options = CreateOptions();
        options.DisableCategory(category);

        Assert.True(new Checker(options).IsClaimable(value), value);
    }

    [Fact]
    public void EveryNewSweepDatasetValueHasExclusiveCategoryOwnership()
    {
        var assembly = typeof(Checker).Assembly;
        var count = 0;

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                || !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            if (!root.TryGetProperty("category", out var categoryProperty)
                || !root.TryGetProperty("values", out var values)
                || !root.TryGetProperty("description", out var descriptionProperty))
            {
                continue;
            }

            var categoryName = categoryProperty.GetString();
            var description = descriptionProperty.GetString();
            var isNewGlobalCore = categoryName is not null
                && NewGlobalCoreCategories.Contains(categoryName)
                && root.TryGetProperty("language", out var language)
                && language.GetString() == "global"
                && description?.StartsWith("Standalone ", StringComparison.Ordinal) == true;
            var isNewEnglishSystemStates = categoryName == "system"
                && root.TryGetProperty("language", out var stateLanguage)
                && stateLanguage.GetString() == "en"
                && description == "Common application-owned lifecycle, availability, processing, and status identifiers.";

            if (!isNewGlobalCore && !isNewEnglishSystemStates)
            {
                continue;
            }

            var category = Enum.Parse<Category>(categoryName!, ignoreCase: true);
            foreach (var valueElement in values.EnumerateArray())
            {
                var value = valueElement.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                AssertExclusiveCategoryOwnership(category, value);
                count++;
            }
        }

        Assert.True(count >= 250, $"Sweep coverage unexpectedly small: {count} exact values checked.");
    }

    [Fact]
    public void ExistingCoreSweepAdditionsHaveExclusiveCategoryOwnership()
    {
        var count = 0;

        foreach (var pair in ExistingCoreSweepValues)
        {
            foreach (var value in pair.Value)
            {
                var result = DatasetChecker.Check(value);
                Assert.True(result.IsReserved, value);
                Assert.Equal(pair.Key.ToString().ToLowerInvariant(), result.Category);
                Assert.Equal(MatchKind.Exact, result.MatchKind);

                AssertExclusiveCategoryOwnership(pair.Key, value);
                count++;
            }
        }

        Assert.Equal(86, count);
    }

    [Fact]
    public void ExistingSecurityOwnershipIsNotReclassifiedByOperationsSweep()
    {
        var result = DatasetChecker.Check("incidentresponse");

        Assert.True(result.IsReserved);
        Assert.Equal("security", result.Category);
    }

    [Fact]
    public void GlobalSweepValuesRemainActiveWithoutEnglishButEnglishStatesDoNot()
    {
        var options = CreateOptions();
        options.RemoveLanguage(Language.English);
        var checker = new Checker(options);

        Assert.Equal("community", checker.Check("member").Category);
        Assert.Equal("governance", checker.Check("vote").Category);
        Assert.Equal("finance", checker.Check("treasury").Category);
        Assert.Equal("official", checker.Check("verified").Category);
        Assert.Equal("commerce", checker.Check("buyer").Category);
        Assert.Equal("security", checker.Check("phishing").Category);
        Assert.True(checker.IsClaimable("active"));
    }

    private static void AssertExclusiveCategoryOwnership(Category category, string value)
    {
        var options = CreateOptions();
        options.DisableCategory(category);
        var result = new Checker(options).Check(value);

        Assert.False(
            result.IsReserved,
            $"'{value}' remains reserved after disabling {category}; another category owns the same exact identifier.");
    }

    private static Options CreateOptions() => new()
    {
        Strictness = Strictness.Standard,
        DisabledRules = StructuralRules
    };
}
