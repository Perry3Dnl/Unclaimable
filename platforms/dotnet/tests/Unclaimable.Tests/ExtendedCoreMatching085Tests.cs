using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class ExtendedCoreMatching085Tests
{
    [Fact]
    public void ExtendedEntriesUseCorePartialMatching()
    {
        const string wrapped = "mycommissarisvandekoningname";

        var coreOnly = new Checker(new Options());
        Assert.True(coreOnly.IsClaimable(wrapped));

        var options = new Options();
        options.UseExtendedData();
        var result = new Checker(options).Check(wrapped);

        Assert.True(result.IsReserved);
        Assert.Equal("regionalrolesnetherlands", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void DisablingCorePartialRulePreventsExtendedSubstringMatches()
    {
        const string wrapped = "mycommissarisvandekoningname";

        var options = new Options();
        options.DisableRule(Rule.PartialMatching);
        options.UseExtendedData();
        var checker = new Checker(options);

        Assert.True(checker.IsClaimable(wrapped));
        Assert.Equal("regionalrolesnetherlands", checker.Check("commissarisvandekoning").Category);
    }

    [Fact]
    public void DisabledExtendedCategoryDoesNotRegisterItsEntries()
    {
        var options = new Options();
        options.UseExtendedData(extended =>
            extended.DisableCategory(ExtendedCategory.RegionalRolesNetherlands));

        Assert.True(new Checker(options).IsClaimable("mycommissarisvandekoningname"));
    }
}
