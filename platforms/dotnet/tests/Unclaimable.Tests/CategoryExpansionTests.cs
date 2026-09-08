using Xunit;

namespace Unclaimable.Tests;

public sealed class CategoryExpansionTests
{
    private static readonly Checker DatasetChecker = new Checker(new Options
    {
        Strictness = Strictness.Standard,
        DisabledRules = Rule.Numbers
                        | Rule.BlockedCharacters
                        | Rule.Whitespace
                        | Rule.LeadingSeparator
                        | Rule.TrailingSeparator
    });

    [Theory]
    [InlineData("infosec", "security")]
    [InlineData("workflowrunner", "automation")]
    [InlineData("privacypolicy", "legal")]
    [InlineData("merchantportal", "commerce")]
    [InlineData("communityambassador", "community")]
    [InlineData("newsroom", "other")]
    public void NewGlobalCategoriesAreRejectedWithExpectedCategory(string value, string category)
    {
        var result = DatasetChecker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(category, result.Category);
    }

    [Theory]
    [InlineData("securityresponsecenter", "security")]
    [InlineData("automationrunner", "automation")]
    [InlineData("legalnotice", "legal")]
    [InlineData("sellerportal", "commerce")]
    [InlineData("communityhub", "community")]
    [InlineData("pressroom", "other")]
    public void NewCategoriesRemainActiveWithoutLocalizedLanguages(string value, string category)
    {
        var options = new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = Rule.Numbers
                            | Rule.BlockedCharacters
                            | Rule.Whitespace
                            | Rule.LeadingSeparator
                            | Rule.TrailingSeparator
        };
        options.RemoveLanguage(Language.English);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(category, result.Category);
    }
}
