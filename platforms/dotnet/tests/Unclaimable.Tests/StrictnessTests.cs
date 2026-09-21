using Xunit;

namespace Unclaimable.Tests;

public sealed class StrictnessTests
{
    [Theory]
    [InlineData("mysuperadminx")]
    [InlineData("myloginverificationteamx")]
    [InlineData("extracustomersupportx")]
    public void StandardStrictnessDisablesPartialMatching(string value)
    {
        var checker = new Checker(new Options
        {
            Strictness = Strictness.Standard
        });

        Assert.True(checker.IsClaimable(value));
    }

    [Theory]
    [InlineData("mysuperadminx", "superadmin")]
    [InlineData("myloginverificationteamx", "loginverificationteam")]
    [InlineData("extracustomersupportx", "customersupport")]
    public void StrictStrictnessRejectsExplicitPartialSafeNames(string value, string expectedMatch)
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
    public void StrictIsTheDefaultButDoesNotTurnEveryBuiltInIntoASubstringRule()
    {
        var options = new Options();
        var checker = new Checker(options);

        Assert.Equal(Strictness.Strict, options.Strictness);
        Assert.Equal(MatchKind.Partial, checker.Check("mysuperadminx").MatchKind);
        Assert.Equal(MatchKind.Partial, checker.Check("supportive").MatchKind);
        Assert.True(checker.IsClaimable("apples"));
        Assert.True(checker.IsClaimable("nikee"));
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
