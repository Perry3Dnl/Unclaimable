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

    [Theory]
    [InlineData("member", "community")]
    [InlineData("membership", "community")]
    [InlineData("vote", "governance")]
    [InlineData("election", "governance")]
    [InlineData("username", "identity")]
    [InlineData("banned", "moderation")]
    [InlineData("operations", "operations")]
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
    [InlineData(Category.Community, "member")]
    [InlineData(Category.Governance, "vote")]
    [InlineData(Category.Identity, "username")]
    [InlineData(Category.Moderation, "banned")]
    [InlineData(Category.Operations, "operations")]
    [InlineData(Category.System, "active")]
    public void NewSweepValuesRespectCategoryDisable(Category category, string value)
    {
        var options = new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = StructuralRules
        };
        options.DisableCategory(category);

        Assert.True(new Checker(options).IsClaimable(value), value);
    }

    [Fact]
    public void GlobalSweepValuesRemainActiveWithoutEnglishButEnglishStatesDoNot()
    {
        var options = new Options
        {
            Strictness = Strictness.Standard,
            DisabledRules = StructuralRules
        };
        options.RemoveLanguage(Language.English);
        var checker = new Checker(options);

        Assert.Equal("community", checker.Check("member").Category);
        Assert.Equal("governance", checker.Check("vote").Category);
        Assert.True(checker.IsClaimable("active"));
    }
}
