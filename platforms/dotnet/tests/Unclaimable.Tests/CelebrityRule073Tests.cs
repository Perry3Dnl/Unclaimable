using System.Reflection;
using Xunit;

namespace Unclaimable.Tests;

public sealed class CelebrityRule073Tests
{
    [Fact]
    public void CelebrityRuleIsEnabledByDefault()
    {
        var options = new Options();
        var result = new Checker(options).Check("donaldtrump");

        Assert.True((options.EnabledOptionalRules & Rule.CelebrityNames) != 0);
        Assert.True(result.IsReserved);
        Assert.Equal("celebrity", result.Category);
    }

    [Theory]
    [InlineData("trump")]
    [InlineData("donaldtrump")]
    [InlineData("taylorswift")]
    [InlineData("beyonce")]
    [InlineData("cristianoronaldo")]
    [InlineData("messi")]
    [InlineData("elonmusk")]
    [InlineData("mrbeast")]
    [InlineData("shahrukhkhan")]
    [InlineData("jackiechan")]
    public void CelebrityRuleRejectsRepresentativeNames(string value)
    {
        var options = new Options().EnableRule(Rule.CelebrityNames);
        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal("celebrity", result.Category);
    }

    [Fact]
    public void CelebrityRuleIsCaseInsensitive()
    {
        var checker = new Checker(new Options().EnableRule(Rule.CelebrityNames));
        var result = checker.Check("TRUMP");

        Assert.True(result.IsReserved);
        Assert.Equal("trump", result.MatchedValue);
        Assert.Equal("celebrity", result.Category);
    }

    [Theory]
    [InlineData("Donald Trump", "donaldtrump")]
    [InlineData("Taylor-Swift", "taylorswift")]
    [InlineData("Cristiano_Ronaldo", "cristianoronaldo")]
    public void CelebrityRuleUsesConfiguredCompactMatching(string value, string expectedMatch)
    {
        var options = new Options()
            .EnableRule(Rule.CelebrityNames)
            .DisableRule(Rule.Whitespace | Rule.BlockedCharacters);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Compact, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
        Assert.Equal("celebrity", result.Category);
    }

    [Fact]
    public void DisablingCompactMatchingStopsCelebrityCompaction()
    {
        var options = new Options()
            .EnableRule(Rule.CelebrityNames)
            .DisableRule(Rule.Whitespace | Rule.CompactMatching);

        Assert.True(new Checker(options).IsClaimable("Donald Trump"));
    }

    [Fact]
    public void CelebrityRuleUsesObfuscationMatching()
    {
        var checker = new Checker(new Options().EnableRule(Rule.CelebrityNames));
        var result = checker.Check("b3yonce");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
        Assert.Equal("beyonce", result.MatchedValue);
        Assert.Equal("celebrity", result.Category);
    }

    [Fact]
    public void CelebrityRuleUsesUnicodeConfusableMatching()
    {
        var checker = new Checker(new Options().EnableRule(Rule.CelebrityNames));
        var result = checker.Check("оbama"); // Cyrillic small o + "bama"

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.UnicodeConfusable, result.MatchKind);
        Assert.Equal("obama", result.MatchedValue);
        Assert.Equal("celebrity", result.Category);
    }

    [Theory]
    [InlineData("trumpet")]
    [InlineData("ronaldofan")]
    [InlineData("messifan")]
    [InlineData("obamacare")]
    public void CelebrityNamesDoNotBecomeSubstringRules(string value)
    {
        var checker = new Checker(new Options().EnableRule(Rule.CelebrityNames));

        Assert.True(checker.IsClaimable(value));
    }

    [Fact]
    public void AllowedIdentifiersDoNotBypassEnabledCelebrityRule()
    {
        var options = new Options().EnableRule(Rule.CelebrityNames);
        options.AllowedIdentifiers.Add("trump");

        var result = new Checker(options).Check("trump");

        Assert.True(result.IsReserved);
        Assert.Equal("celebrity", result.Category);
    }

    [Fact]
    public void CelebrityRuleCanBeDisabledAfterBeingEnabled()
    {
        var options = new Options().EnableRule(Rule.CelebrityNames);
        options.DisableRule(Rule.CelebrityNames);

        Assert.Equal(Rule.None, options.EnabledOptionalRules & Rule.CelebrityNames);
        Assert.True(new Checker(options).IsClaimable("donaldtrump"));
    }

    [Fact]
    public void CelebrityRuleConfigurationIsCapturedByChecker()
    {
        var options = new Options().EnableRule(Rule.CelebrityNames);
        var checker = new Checker(options);

        options.DisableRule(Rule.CelebrityNames);

        Assert.True(checker.Check("trump").IsReserved);
        Assert.True(new Checker(options).IsClaimable("donaldtrump"));
    }

    [Fact]
    public void CelebrityStarterListContainsExactlyOneHundredUniqueNames()
    {
        var dataType = typeof(Checker).Assembly.GetType("Unclaimable.CelebrityData", throwOnError: true)!;
        var field = dataType.GetField("Names", BindingFlags.Static | BindingFlags.NonPublic)!;
        var values = Assert.IsType<string[]>(field.GetValue(null));

        Assert.Equal(100, values.Length);
        Assert.Equal(100, values.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("trump", values);
        Assert.Contains("donaldtrump", values);
    }
}
