using Xunit;

namespace Unclaimable.Tests;

public sealed class ExpandedDatasetTests
{
    [Theory]
    [InlineData("superadmin", "roles")]
    [InlineData("communitymoderator", "roles")]
    [InlineData("sysadmin", "roles")]
    [InlineData("trust and safety", "support")]
    [InlineData("account recovery", "support")]
    [InlineData("fraudprevention", "support")]
    [InlineData("serviceaccount", "system")]
    [InlineData("webhook", "system")]
    [InlineData("localhost", "system")]
    [InlineData("cloudflare", "technology")]
    [InlineData("anthropic", "technology")]
    [InlineData("postgresql", "technology")]
    [InlineData("atlassian", "technology")]
    [InlineData("americanexpress", "brands")]
    [InlineData("postnl", "brands")]
    [InlineData("lamborghini", "brands")]
    [InlineData("underarmour", "brands")]
    [InlineData("qatarairways", "brands")]
    public void ExpandedReservedNamesAreRejected(string value, string category)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(category, result.Category);
    }

    [Theory]
    [InlineData("ordinary-person")]
    [InlineData("bluegarden")]
    [InlineData("mountainreader")]
    [InlineData("friendly-coder-42")]
    public void OrdinaryNamesStillRemainClaimable(string value)
    {
        Assert.True(UnclaimableChecker.Default.IsClaimable(value));
    }
}
