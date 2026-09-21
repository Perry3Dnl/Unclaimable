using Xunit;

namespace Unclaimable.Tests;

public sealed class GeographyRule072Tests
{
    [Fact]
    public void GeographyAndNumberRulesAreEnabledByDefault()
    {
        var options = new Options();
        var checker = new Checker(options);

        Assert.True((options.EnabledOptionalRules & Rule.CountryNames) != 0);
        Assert.True((options.EnabledOptionalRules & Rule.PopularCityNames) != 0);
        Assert.Equal(Rule.None, options.DisabledRules);
        Assert.Equal(MatchKind.CountryName, checker.Check("france").MatchKind);
        Assert.Equal(MatchKind.PopularCityName, checker.Check("amsterdam").MatchKind);
        Assert.Equal(MatchKind.NumbersNotAllowed, checker.Check("user7").MatchKind);
        Assert.Equal(MatchKind.NumbersNotAllowed, checker.Check("123456789").MatchKind);
    }

    [Theory]
    [InlineData("france")]
    [InlineData("netherlands")]
    [InlineData("brazil")]
    [InlineData("southafrica")]
    [InlineData("japan")]
    [InlineData("newzealand")]
    [InlineData("côtedivoire")]
    [InlineData("turkiye")]
    [InlineData("usa")]
    [InlineData("uae")]
    public void CountryRuleRejectsRepresentativeCountryNames(string value)
    {
        var options = new Options().EnableRule(Rule.CountryNames);
        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.CountryName, result.MatchKind);
        Assert.Null(result.Category);
    }

    [Theory]
    [InlineData("amsterdam")]
    [InlineData("newyork")]
    [InlineData("london")]
    [InlineData("tokyo")]
    [InlineData("dubai")]
    [InlineData("capetown")]
    [InlineData("saopaulo")]
    [InlineData("sydney")]
    [InlineData("singapore")]
    [InlineData("mumbai")]
    public void PopularCityRuleRejectsRepresentativeCityNames(string value)
    {
        var options = new Options().EnableRule(Rule.PopularCityNames);
        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.PopularCityName, result.MatchKind);
        Assert.Null(result.Category);
    }

    [Fact]
    public void GeographyRulesCanBeEnabledIndependently()
    {
        var countries = new Checker(new Options().DisableRule(Rule.PopularCityNames));
        var cities = new Checker(new Options().DisableRule(Rule.CountryNames));

        Assert.Equal(MatchKind.CountryName, countries.Check("france").MatchKind);
        Assert.True(countries.IsClaimable("amsterdam"));

        Assert.Equal(MatchKind.PopularCityName, cities.Check("amsterdam").MatchKind);
        Assert.True(cities.IsClaimable("france"));
    }

    [Fact]
    public void CountryRuleWinsForCountryCityOverlap()
    {
        var options = new Options().EnableRule(Rule.CountryNames | Rule.PopularCityNames);
        var result = new Checker(options).Check("singapore");

        Assert.Equal(MatchKind.CountryName, result.MatchKind);
    }

    [Theory]
    [InlineData("United Kingdom", MatchKind.CountryName)]
    [InlineData("New York", MatchKind.PopularCityName)]
    [InlineData("Rio-de-Janeiro", MatchKind.PopularCityName)]
    public void GeographyRulesUseWholeIdentifierCompactMatching(string value, MatchKind expected)
    {
        var options = new Options()
            .EnableRule(Rule.CountryNames | Rule.PopularCityNames)
            .DisableRule(Rule.Whitespace | Rule.BlockedCharacters);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(expected, result.MatchKind);
    }

    [Fact]
    public void DisablingCompactMatchingAlsoDisablesGeographyCompaction()
    {
        var options = new Options()
            .EnableRule(Rule.PopularCityNames)
            .DisableRule(Rule.Whitespace | Rule.CompactMatching);

        Assert.True(new Checker(options).IsClaimable("New York"));
    }

    [Theory]
    [InlineData("francelover")]
    [InlineData("newyorker")]
    [InlineData("amsterdammer")]
    [InlineData("tokyostory")]
    public void GeographyRulesDoNotBecomeSubstringRules(string value)
    {
        var options = new Options().EnableRule(Rule.CountryNames | Rule.PopularCityNames);

        Assert.True(new Checker(options).IsClaimable(value));
    }

    [Fact]
    public void AllowedIdentifiersDoNotBypassEnabledGeographyRules()
    {
        var options = new Options().EnableRule(Rule.CountryNames);
        options.AllowedIdentifiers.Add("france");

        var result = new Checker(options).Check("france");

        Assert.Equal(MatchKind.CountryName, result.MatchKind);
    }

    [Fact]
    public void DirectDisabledRulesMaskStillWinsAfterOptionalRuleWasEnabled()
    {
        var options = new Options().EnableRule(Rule.CountryNames);
        options.DisabledRules |= Rule.CountryNames;

        Assert.Equal(Rule.None, options.EnabledOptionalRules & Rule.CountryNames);
        Assert.True(new Checker(options).IsClaimable("france"));
    }

    [Fact]
    public void EnableAndDisableRuleHelpersWorkForExistingRulesToo()
    {
        var options = new Options();
        Assert.False(new Checker(options).IsClaimable("user7"));

        options.DisableRule(Rule.Numbers);
        Assert.True(new Checker(options).IsClaimable("user7"));

        options.EnableRule(Rule.Numbers);
        Assert.False(new Checker(options).IsClaimable("user7"));
    }

    [Fact]
    public void GeographyRuleConfigurationIsCapturedByChecker()
    {
        var options = new Options().EnableRule(Rule.CountryNames);
        var checker = new Checker(options);

        options.DisableRule(Rule.CountryNames);

        Assert.Equal(MatchKind.CountryName, checker.Check("france").MatchKind);
        Assert.True(new Checker(options).IsClaimable("france"));
    }

    [Fact]
    public void UnsupportedRuleBitsAreRejectedByHelpers()
    {
        var options = new Options();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.EnableRule((Rule)(1 << 30)));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DisableRule((Rule)(1 << 30)));
    }
}
