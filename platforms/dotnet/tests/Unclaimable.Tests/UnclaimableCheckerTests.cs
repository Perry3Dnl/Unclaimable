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
    [InlineData("apple")]
    [InlineData("Microsoft")]
    [InlineData("LINUX")]
    [InlineData("github")]
    public void TechnologyNamesAreRejected(string value)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("technology", result.Category);
    }

    [Theory]
    [InlineData("nike")]
    [InlineData("Adidas")]
    [InlineData("coca-cola")]
    [InlineData("Louis_Vuitton")]
    public void BrandNamesAreRejected(string value)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal("brands", result.Category);
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
    [InlineData("N1ke", "nike", "brands")]
    [InlineData("N1k3", "nike", "brands")]
    [InlineData("M1crosoft", "microsoft", "technology")]
    [InlineData("G00gle", "google", "technology")]
    [InlineData("@pple", "apple", "technology")]
    [InlineData("app1e", "apple", "technology")]
    [InlineData("r00t", "root", "roles")]
    [InlineData("c0ca-c0la", "coca cola", "brands")]
    public void ObfuscationMatchingBlocksCommonLeetspeak(
        string value,
        string matchedValue,
        string category)
    {
        var result = UnclaimableChecker.Default.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(matchedValue, result.MatchedValue);
        Assert.Equal(category, result.Category);
        Assert.Equal(UnclaimableMatchKind.Obfuscated, result.MatchKind);
    }

    [Theory]
    [InlineData("administrator2")]
    [InlineData("supportive")]
    [InlineData("systematic")]
    [InlineData("ordinary-user")]
    [InlineData("ordinary123")]
    [InlineData("apples")]
    [InlineData("nikee")]
    public void SimilarButDifferentNamesRemainClaimable(string value)
    {
        Assert.True(UnclaimableChecker.Default.IsClaimable(value));
    }

    [Fact]
    public void ApplicationSpecificNamesCanBeAddedWithoutChangingGlobalData()
    {
        var options = new UnclaimableOptions();
        options.AdditionalReserved.Add("examplebrand");
        options.AdditionalReserved.Add("internalbot");

        var checker = new UnclaimableChecker(options);

        Assert.True(checker.IsReserved("ExampleBrand"));
        Assert.True(checker.IsReserved("internal-bot"));
        Assert.Equal("custom", checker.Check("examplebrand").Category);
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

    [Fact]
    public void ObfuscationMatchingCanBeDisabled()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            ObfuscationMatching = false
        });

        Assert.True(checker.IsReserved("nike"));
        Assert.False(checker.IsReserved("N1k3"));
    }
}
