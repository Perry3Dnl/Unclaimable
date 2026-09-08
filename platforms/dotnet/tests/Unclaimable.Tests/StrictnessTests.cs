using Xunit;

namespace Unclaimable.Tests;

public sealed class StrictnessTests
{
    [Theory]
    [InlineData("supportive")]
    [InlineData("apples")]
    [InlineData("nikee")]
    public void StandardStrictnessDisablesPartialMatching(string value)
    {
        var checker = new Checker(new Options
        {
            Strictness = Strictness.Standard
        });

        Assert.True(checker.IsClaimable(value));
    }

    [Theory]
    [InlineData("supportive", "support")]
    [InlineData("apples", "apple")]
    [InlineData("nikee", "nike")]
    public void StrictStrictnessRejectsEmbeddedReservedNames(string value, string expectedMatch)
    {
        var checker = new Checker(new Options
        {
            Strictness = Strictness.Strict
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
    }

    [Fact]
    public void StrictIsTheDefault()
    {
        var options = new Options();

        Assert.Equal(Strictness.Strict, options.Strictness);
        Assert.True(new Checker(options).IsReserved("supportive"));
    }

    [Fact]
    public void StrictStrictnessAlsoAppliesToAdditionalReservedValues()
    {
        var options = new Options();
        options.AdditionalReserved.Add("examplebrand");

        var checker = new Checker(options);
        var result = checker.Check("oldexamplebrand");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("examplebrand", result.MatchedValue);
        Assert.Equal("custom", result.Category);
    }

    [Fact]
    public void StrictStrictnessStillHonorsPartialMinimumLength()
    {
        var checker = new Checker(new Options());

        Assert.True(checker.IsReserved("api"));
        Assert.True(checker.IsClaimable("rapid"));
    }

    [Fact]
    public void StrictStrictnessDoesNotImplicitlyEnableProfanityPartialMatching()
    {
        var checker = new Checker(new Options());

        Assert.True(checker.IsReserved("cock"));
        Assert.True(checker.IsClaimable("cocktail"));
    }

    [Fact]
    public void ProfanityPartialMatchingCanStillBeEnabledExplicitlyInStrictMode()
    {
        var checker = new Checker(new Options
        {
            ProfanityPartialMatching = true
        });

        var result = checker.Check("cocktail");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal("cock", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
    }
}
