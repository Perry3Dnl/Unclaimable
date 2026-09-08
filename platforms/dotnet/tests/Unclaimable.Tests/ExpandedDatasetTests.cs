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
    [InlineData("workspaceadministrator", "roles")]
    [InlineData("trust and safety", "support")]
    [InlineData("account recovery", "support")]
    [InlineData("fraudprevention", "support")]
    [InlineData("identityverification", "support")]
    [InlineData("serviceaccount", "system")]
    [InlineData("webhook", "system")]
    [InlineData("localhost", "system")]
    [InlineData("featureflags", "system")]
    [InlineData("comment", "system")]
    [InlineData("comments", "system")]
    [InlineData("blog", "system")]
    [InlineData("blogs", "system")]
    [InlineData("post", "system")]
    [InlineData("posts", "system")]
    [InlineData("search", "system")]
    [InlineData("report", "system")]
    [InlineData("reports", "system")]
    [InlineData("article", "system")]
    [InlineData("page", "system")]
    [InlineData("forum", "system")]
    [InlineData("thread", "system")]
    [InlineData("message", "system")]
    [InlineData("inbox", "system")]
    [InlineData("news", "system")]
    [InlineData("media", "system")]
    [InlineData("category", "system")]
    [InlineData("tag", "system")]
    [InlineData("draft", "system")]
    [InlineData("archive", "system")]
    [InlineData("cloudflare", "technology")]
    [InlineData("anthropic", "technology")]
    [InlineData("postgresql", "technology")]
    [InlineData("atlassian", "technology")]
    [InlineData("homeassistant", "technology")]
    [InlineData("americanexpress", "brands")]
    [InlineData("postnl", "brands")]
    [InlineData("lamborghini", "brands")]
    [InlineData("underarmour", "brands")]
    [InlineData("qatarairways", "brands")]
    [InlineData("centraalbeheer", "brands")]
    [InlineData("fuckboy", "profanity")]
    [InlineData("fuckboylover", "profanity")]
    [InlineData("fuckwaffle", "profanity")]
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
