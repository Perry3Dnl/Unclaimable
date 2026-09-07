using Xunit;

namespace Unclaimable.Tests;

public sealed class StrictnessTests
{
    [Theory]
    [InlineData("admin2")]
    [InlineData("old-admin")]
    [InlineData("admin-old")]
    [InlineData("administrator2")]
    public void StandardStrictnessKeepsPartialMatchingOptIn(string value)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Standard
        });

        Assert.True(checker.IsClaimable(value));
    }

    [Theory]
    [InlineData("admin2", "admin")]
    [InlineData("old-admin", "admin")]
    [InlineData("admin-old", "admin")]
    [InlineData("administrator2", "administrator")]
    public void StrictStrictnessRejectsEmbeddedReservedNames(string value, string expectedMatch)
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.False(result.IsClaimable);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
    }

    [Fact]
    public void StrictStrictnessAlsoAppliesToAdditionalReservedValues()
    {
        var options = new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict
        };
        options.AdditionalReserved.Add("examplebrand");

        var checker = new UnclaimableChecker(options);

        var result = checker.Check("old-examplebrand");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal("examplebrand", result.MatchedValue);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void StrictStrictnessStillHonorsPartialMinimumLength()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict
        });

        Assert.True(checker.IsReserved("api"));
        Assert.True(checker.IsClaimable("api123"));
        Assert.True(checker.IsClaimable("rapid"));
    }

    [Fact]
    public void StrictStrictnessDoesNotImplicitlyEnableProfanityPartialMatching()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            ProfanityMatching = true
        });

        Assert.True(checker.IsReserved("cock"));
        Assert.True(checker.IsClaimable("cocktail"));
    }

    [Fact]
    public void ProfanityPartialMatchingCanStillBeEnabledExplicitlyInStrictMode()
    {
        var checker = new UnclaimableChecker(new UnclaimableOptions
        {
            Strictness = UnclaimableStrictness.Strict,
            ProfanityMatching = true,
            ProfanityPartialMatching = true
        });

        var result = checker.Check("cocktail");

        Assert.True(result.IsReserved);
        Assert.Equal(UnclaimableMatchKind.Partial, result.MatchKind);
        Assert.Equal("cock", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
    }
}
