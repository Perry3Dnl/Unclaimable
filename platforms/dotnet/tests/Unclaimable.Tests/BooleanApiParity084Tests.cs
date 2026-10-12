using Xunit;

namespace Unclaimable.Tests;

public sealed class BooleanApiParity084Tests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("zqvxpioneer")]
    [InlineData("admin")]
    [InlineData("N1ke")]
    [InlineData("1111")]
    [InlineData("abababab")]
    [InlineData("_admin_")]
    [InlineData("\u0430dmin")]
    public void BooleanApisMatchCheckForRepresentativeDefaultInputs(string? value)
    {
        var checker = new Checker();
        var result = checker.Check(value);

        Assert.Equal(result.IsReserved, checker.IsReserved(value));
        Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
    }

    [Fact]
    public void BooleanApisMatchCheckWithCustomAndAllowedIdentifiers()
    {
        var options = new Options();
        options.AdditionalReserved.Add("zzcustomguard");
        options.AllowedIdentifiers.Add("admin");

        var checker = new Checker(options);
        var values = new[]
        {
            "admin",
            "zzcustomguard",
            "prefixzzcustomguardsuffix",
            "zqvxpioneer"
        };

        foreach (var value in values)
        {
            var result = checker.Check(value);
            Assert.Equal(result.IsReserved, checker.IsReserved(value));
            Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
        }
    }

    [Fact]
    public void BooleanApisCoverOptionalRuleFastPaths()
    {
        var options = new Options();
        options.DisableRule(
            Rule.BlockedCharacters
            | Rule.Whitespace
            | Rule.LeadingSeparator
            | Rule.TrailingSeparator);

        var checker = new Checker(options);
        foreach (var value in new[]
                 {
                     "france",
                     "london",
                     "dutch",
                     "new-zealand",
                     "new-york",
                     "new-zealander",
                     "\u0430merican",
                     "am3rican"
                 })
        {
            AssertReservedParity(checker, value);
        }
    }

    [Fact]
    public void BooleanApisCoverAllowedCustomFastPaths()
    {
        const string reserved = "zzcustomalpha";
        var values = new[]
        {
            reserved,
            "zz-customalpha",
            "xxzzcustomalphayy",
            "zzcustom\u0430lpha",
            "zzcustom4lpha"
        };

        var options = new Options();
        options.DisableRule(Rule.BlockedCharacters);
        options.AdditionalReserved.Add(reserved);
        foreach (var value in values)
        {
            options.AllowedIdentifiers.Add(value);
        }

        var checker = new Checker(options);
        foreach (var value in values)
        {
            AssertReservedParity(checker, value);
        }
    }

    [Fact]
    public void BooleanApisCoverDisabledObfuscationAndEmptyNormalizedValue()
    {
        var options = new Options
        {
            ObfuscationMatching = false,
            UnicodeConfusableMatching = false,
            RejectInvisibleOnlyIdentifiers = false,
            RejectControlCharacters = false,
            RejectFormatCharacters = false
        };
        options.DisableRule(
            Rule.MinimumLength
            | Rule.Whitespace
            | Rule.BlockedCharacters
            | Rule.LeadingSeparator
            | Rule.TrailingSeparator);
        options.AllowedIdentifiers.Add("zzallowedonly");

        var checker = new Checker(options);

        AssertParity(checker, "zqvxpioneer");
        AssertParity(checker, "zzallowedonly");
        AssertParity(checker, "   ");
    }

    [Fact]
    public void BooleanApisCoverPolicyFailureFastPaths()
    {
        AssertParity(new Checker(), "\uD800");
        AssertParity(new Checker(), "ab");
        AssertParity(new Checker(), new string('a', 33));
        AssertParity(new Checker(), "abc_");

        var numbers = new Options();
        numbers.EnableRule(Rule.Numbers);
        AssertParity(new Checker(numbers), "abc1");

        var ascii = new Options { AsciiOnly = true };
        AssertParity(new Checker(ascii), "caf\u00E9");
    }

    [Fact]
    public void BooleanApisCoverDisabledOptionalRuleConstruction()
    {
        var options = new Options();
        options.DisableRule(
            Rule.CountryNames
            | Rule.PopularCityNames
            | Rule.CelebrityNames
            | Rule.Nationalities
            | Rule.Currencies
            | Rule.Religions
            | Rule.Landmarks
            | Rule.Events
            | Rule.Awards
            | Rule.FictionalCharacters
            | Rule.Franchises
            | Rule.Professions
            | Rule.Military);

        AssertParity(new Checker(options), "zqvxpioneer");
    }

    [Fact]
    public void BooleanApisCoverExactOnlyCustomReservation()
    {
        var options = new Options().Reserve("zzexactguard", ReservedMatchMode.Exact);
        AssertReservedParity(new Checker(options), "zzexactguard");
    }

    private static void AssertReservedParity(Checker checker, string value)
    {
        var result = checker.Check(value);
        Assert.True(result.IsReserved);
        Assert.True(checker.IsReserved(value));
        Assert.False(checker.IsClaimable(value));
    }

    private static void AssertParity(Checker checker, string value)
    {
        var result = checker.Check(value);
        Assert.Equal(result.IsReserved, checker.IsReserved(value));
        Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
    }

    [Fact]
    public void BooleanApisMatchCheckAtHighObfuscationSensitivity()
    {
        var options = new Options { ObfuscationSensitivity = ObfuscationSensitivity.High };
        options.DisablePattern(Pattern.Repeated);
        var checker = new Checker(options);

        foreach (var value in new[] { "admmmin", "admin", "N1ke" })
        {
            var result = checker.Check(value);
            Assert.Equal(result.IsReserved, checker.IsReserved(value));
            Assert.Equal(result.IsClaimable, checker.IsClaimable(value));
        }
    }
}
