using Xunit;

namespace Unclaimable.Tests;

public sealed class ExpandedDatasetTests
{
    private static readonly UnclaimableChecker DatasetChecker = new UnclaimableChecker(new UnclaimableOptions
    {
        Strictness = UnclaimableStrictness.Standard,
        DisabledRules = UnclaimableRule.Numbers
                        | UnclaimableRule.BlockedCharacters
                        | UnclaimableRule.Whitespace
                        | UnclaimableRule.LeadingSeparator
                        | UnclaimableRule.TrailingSeparator
    });

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
        var result = DatasetChecker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(category, result.Category);
    }

    [Theory]
    [InlineData("ordinary-person")]
    [InlineData("bluegarden")]
    [InlineData("mountainreader")]
    [InlineData("friendly-coder-42")]
    public void OrdinaryNamesRemainClaimableWhenStructuralPolicyIsRelaxed(string value)
    {
        Assert.True(DatasetChecker.IsClaimable(value));
    }
}
