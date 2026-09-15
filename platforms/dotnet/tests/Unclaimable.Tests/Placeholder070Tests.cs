using Xunit;

namespace Unclaimable.Tests;

public sealed class Placeholder070Tests
{
    [Theory]
    [InlineData("null")]
    [InlineData("NULL")]
    [InlineData("Null")]
    [InlineData("undefined")]
    [InlineData("empty")]
    [InlineData("emptyvalue")]
    [InlineData("none")]
    [InlineData("nil")]
    [InlineData("unset")]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("notset")]
    public void PlaceholderLikeIdentifiersAreReservedByDefault(string value)
    {
        var result = new Checker().Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal("placeholders", result.Category);
    }

    [Fact]
    public void PlaceholderMatchingUsesNormalNfkcAndCaseNormalization()
    {
        var result = new Checker().Check("ＮＵＬＬ");

        Assert.True(result.IsReserved);
        Assert.Equal("null", result.MatchedValue);
        Assert.Equal("placeholders", result.Category);
    }

    [Fact]
    public void DisablingPlaceholderCategoryRemovesOnlyThatDataset()
    {
        var options = new Options();
        options.DisableCategory(Category.Placeholders);

        var checker = new Checker(options);

        Assert.True(checker.Check("null").IsClaimable);

        var admin = checker.Check("admin");
        Assert.True(admin.IsReserved);
        Assert.NotEqual("placeholders", admin.Category);
    }

    [Fact]
    public void ReEnablingPlaceholderCategoryRestoresReservation()
    {
        var options = new Options();
        options.DisableCategory(Category.Placeholders);
        options.EnableCategory(Category.Placeholders);

        var result = new Checker(options).Check("undefined");

        Assert.True(result.IsReserved);
        Assert.Equal("placeholders", result.Category);
    }

    [Fact]
    public void AllowingUppercasePatternDoesNotClearUppercasePlaceholder()
    {
        var options = new Options();
        options.DisablePattern(Pattern.UppercaseOnly);

        var result = new Checker(options).Check("NULL");

        Assert.True(result.IsReserved);
        Assert.Equal("placeholders", result.Category);
    }

    [Fact]
    public void ActualNullInputIsDistinctFromTheLiteralNullIdentifier()
    {
        var checker = new Checker();

        Assert.True(checker.Check(null).IsClaimable);
        Assert.True(checker.Check("null").IsReserved);
    }
}
