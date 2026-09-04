using Xunit;

namespace Unclaimable.Tests;

public sealed class UnclaimableCheckerTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData(" ADMIN ")]
    [InlineData("moderator")]
    [InlineData("support")]
    [InlineData("system")]
    public void BuiltInReservedNamesAreRejected(string value)
    {
        Assert.True(UnclaimableChecker.Default.IsReserved(value));
        Assert.False(UnclaimableChecker.Default.IsClaimable(value));
    }

    [Theory]
    [InlineData("customer-service")]
    [InlineData("customer_service")]
    [InlineData("customer.service")]
    [InlineData("customer service")]
    public void CompactMatchingBlocksSeparatorVariants(string value)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("support", result.Category);
    }

    [Theory]
    [InlineData("administrator2")]
    [InlineData("supportive")]
    [InlineData("systematic")]
    [InlineData("ordinary-user")]
    public void SimilarButDifferentNamesRemainClaimable(string value)
    {
        Assert.True(UnclaimableChecker.Default.IsClaimable(value));
    }

    [Fact]
    public void ApplicationSpecificNamesCanBeAddedWithoutChangingGlobalData()
    {
        var options = new UnclaimableOptions();
        options.AdditionalReserved.Add("thesugarlook");
        options.AdditionalReserved.Add("sitecheckuser");

        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("TheSugarLook"));
        Assert.True(checker.IsReserved("site-check-user"));
        Assert.Equal("custom", checker.Check("thesugarlook").Category);
    }

    [Fact]
    public void CompactMatchingCanBeDisabled()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            CompactMatching = false
        });

        Assert.True(checker.IsReserved("customer service"));
        Assert.False(checker.IsReserved("customer-service"));
    }
}
