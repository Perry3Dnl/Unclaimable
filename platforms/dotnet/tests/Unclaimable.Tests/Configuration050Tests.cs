using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class Configuration050Tests
{
    [Fact]
    public void DisablingBrandsAllowsBrandOnlyMatchesButLeavesOtherCategoriesActive()
    {
        var options = new Options();
        options.DisableCategory(Category.Brands);

        var checker = new Checker(options);

        Assert.True(checker.Check("nike").IsClaimable);

        var support = checker.Check("supportive");
        Assert.True(support.IsReserved);
        Assert.Equal("support", support.Category);
    }

    [Fact]
    public void DisabledCategoryIsAbsentFromExactPartialCompactAndObfuscationMatching()
    {
        var options = new Options
        {
            DisabledRules = Rule.Numbers
                            | Rule.BlockedCharacters
                            | Rule.Whitespace
                            | Rule.LeadingSeparator
                            | Rule.TrailingSeparator
        };
        options.DisableCategory(Category.Brands);

        var checker = new Checker(options);

        Assert.True(checker.Check("nike").IsClaimable);
        Assert.True(checker.Check("nikee").IsClaimable);
        Assert.True(checker.Check("n.i.k.e").IsClaimable);
        Assert.True(checker.Check("N1k3").IsClaimable);
    }

    [Fact]
    public void DisabledCategoryIsAbsentFromUnicodeConfusableMatching()
    {
        var options = new Options();
        options.DisableCategory(Category.Technology);

        var checker = new Checker(options);

        Assert.True(checker.Check("\u0430pple").IsClaimable);
    }

    [Fact]
    public void AllowedIdentifierBypassesOnlyThatCompleteBuiltInIdentifier()
    {
        var options = new Options();
        options.AllowedIdentifiers.Add("supportive");

        var checker = new Checker(options);

        Assert.True(checker.Check("supportive").IsClaimable);
        Assert.True(checker.Check("supportiveadmin").IsReserved);
    }

    [Fact]
    public void AllowedIdentifierUsesTrimNfkcAndInvariantCaseNormalization()
    {
        var options = new Options
        {
            DisabledRules = Rule.Whitespace
        };
        options.AllowedIdentifiers.Add("apple");

        var checker = new Checker(options);

        Assert.True(checker.Check("ＡＰＰＬＥ").IsClaimable);
        Assert.True(checker.Check(" APPLE ").IsClaimable);
    }

    [Fact]
    public void AllowedIdentifierDoesNotAllowObfuscatedVariants()
    {
        var options = new Options
        {
            DisabledRules = Rule.Numbers
        };
        options.AllowedIdentifiers.Add("supportive");

        var result = new Checker(options).Check("supp0rtive");

        Assert.True(result.IsReserved);
    }

    [Fact]
    public void AllowedIdentifierStillFailsStructuralValidation()
    {
        var options = new Options
        {
            MinimumLength = 20
        };
        options.AllowedIdentifiers.Add("supportive");

        var result = new Checker(options).Check("supportive");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.TooShort, result.MatchKind);
        Assert.Null(result.Category);
    }

    [Fact]
    public void AdditionalReservedTakesPrecedenceOverAllowedIdentifier()
    {
        var options = new Options();
        options.AllowedIdentifiers.Add("supportive");
        options.AdditionalReserved.Add("support");

        var result = new Checker(options).Check("supportive");

        Assert.True(result.IsReserved);
        Assert.Equal("custom", result.Category);
        Assert.Equal("support", result.MatchedValue);
    }

    [Fact]
    public void ExactReservationRejectsWholeIdentifierButAllowsOrdinaryCompound()
    {
        var options = new Options();
        options.Reserve("acme", matching: ReservedMatchMode.Exact);

        var checker = new Checker(options);

        var exact = checker.Check("ACME");
        Assert.True(exact.IsReserved);
        Assert.Equal("custom", exact.Category);
        Assert.Equal(MatchKind.Exact, exact.MatchKind);

        Assert.True(checker.Check("acmeorchid").IsClaimable);
    }

    [Fact]
    public void ExactReservationUsesTrimNfkcAndInvariantCaseNormalizationWithoutBroadeningTheMatch()
    {
        var options = new Options
        {
            DisabledRules = Rule.Whitespace
        };
        options.Reserve("café", matching: ReservedMatchMode.Exact);
        options.Reserve("acme.bot", matching: ReservedMatchMode.Exact);
        options.Reserve("acme", matching: ReservedMatchMode.Exact);

        var checker = new Checker(options);

        Assert.True(checker.Check("CAFE\u0301").IsReserved);
        Assert.True(checker.Check("ＡＣＭＥ").IsReserved);
        Assert.True(checker.Check(" acme ").IsReserved);
        Assert.True(checker.Check("ACME.BOT").IsReserved);
        Assert.True(checker.Check("caféteria").IsClaimable);
        Assert.True(checker.Check("acmebot").IsClaimable);
    }

    [Fact]
    public void ExactReservationTakesPrecedenceOverAllowedIdentifier()
    {
        var options = new Options();
        options.AllowedIdentifiers.Add("acme");
        options.Reserve("acme", matching: ReservedMatchMode.Exact);

        var result = new Checker(options).Check("acme");

        Assert.True(result.IsReserved);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void DefaultReservationKeepsTheExistingMatchingPipeline()
    {
        var options = new Options();
        options.Reserve("zorbium", matching: ReservedMatchMode.Default);

        var checker = new Checker(options);

        Assert.True(checker.Check("zorbium").IsReserved);

        var compound = checker.Check("zorbiumgarden");
        Assert.True(compound.IsReserved);
        Assert.Equal("custom", compound.Category);
        Assert.Equal(MatchKind.Partial, compound.MatchKind);
    }

    [Fact]
    public void DisablingOneCategoryDoesNotRemoveAValueOwnedByAnotherEnabledCategory()
    {
        var duplicate = FindStructurallySafeDuplicateValue();
        Assert.NotNull(duplicate);

        var defaultResult = new Checker().Check(duplicate!.Value.Value);
        Assert.True(defaultResult.IsReserved);
        Assert.NotNull(defaultResult.Category);
        Assert.True(Enum.TryParse<Category>(defaultResult.Category, ignoreCase: true, out var winningCategory));
        Assert.Contains(defaultResult.Category!, duplicate.Value.Categories);

        var options = new Options();
        options.DisableCategory(winningCategory);

        var result = new Checker(options).Check(duplicate.Value.Value);

        Assert.True(result.IsReserved);
        Assert.NotEqual(defaultResult.Category, result.Category);
        Assert.Contains(result.Category!, duplicate.Value.Categories);
    }

    [Fact]
    public void BuiltInDatasetCategoriesExactlyMatchCategoryEnum()
    {
        var actualCategories = new HashSet<string>(StringComparer.Ordinal);
        var assembly = typeof(Checker).Assembly;

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);

            using var document = JsonDocument.Parse(stream!);
            if (!document.RootElement.TryGetProperty("category", out var categoryElement))
            {
                continue;
            }

            var category = categoryElement.GetString();
            if (!string.IsNullOrWhiteSpace(category))
            {
                actualCategories.Add(category);
            }
        }

        var expectedCategories = Enum.GetNames<Category>()
            .Select(name => name.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            expectedCategories.OrderBy(value => value, StringComparer.Ordinal),
            actualCategories.OrderBy(value => value, StringComparer.Ordinal));
    }

    [Fact]
    public void ConfigurationIsCapturedWhenCheckerIsConstructed()
    {
        var options = new Options();
        options.DisableCategory(Category.Brands);
        options.AllowedIdentifiers.Add("supportive");

        var captured = new Checker(options);

        options.EnableCategory(Category.Brands);
        options.AllowedIdentifiers.Clear();

        Assert.True(captured.Check("nike").IsClaimable);
        Assert.True(captured.Check("supportive").IsClaimable);

        var rebuilt = new Checker(options);
        Assert.True(rebuilt.Check("nike").IsReserved);
        Assert.True(rebuilt.Check("supportive").IsReserved);
    }

    [Theory]
    [InlineData("admin", true, "roles")]
    [InlineData("superadmin", true, "roles")]
    [InlineData("supportive", true, "support")]
    [InlineData("Apple", true, "technology")]
    [InlineData("nike", true, "brands")]
    [InlineData("ordinaryname", false, null)]
    [InlineData("bluegarden", false, null)]
    public void NoNewConfigurationPreservesRepresentative040Outcomes(string value, bool reserved, string? category)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.Equal(reserved, result.IsReserved);
        Assert.Equal(category, result.Category);
    }

    [Fact]
    public void ConfigurationMethodsRejectUndefinedEnumValues()
    {
        var options = new Options();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.DisableCategory((Category)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.EnableCategory((Category)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Reserve("acme", (ReservedMatchMode)999));
    }

    private static (string Value, HashSet<string> Categories)? FindStructurallySafeDuplicateValue()
    {
        var categoriesByValue = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
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
                || !Enum.TryParse<Category>(category, ignoreCase: true, out _)
                || !document.RootElement.TryGetProperty("values", out var valuesElement))
            {
                continue;
            }

            foreach (var valueElement in valuesElement.EnumerateArray())
            {
                var value = valueElement.GetString();
                if (value is null
                    || value.Length < 3
                    || value.Length > 32
                    || value.Any(character => !char.IsLetter(character)))
                {
                    continue;
                }

                if (!categoriesByValue.TryGetValue(value, out var categories))
                {
                    categories = new HashSet<string>(StringComparer.Ordinal);
                    categoriesByValue.Add(value, categories);
                }

                categories.Add(category);
            }
        }

        foreach (var pair in categoriesByValue.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (pair.Value.Count > 1)
            {
                return (pair.Key, pair.Value);
            }
        }

        return null;
    }
}
