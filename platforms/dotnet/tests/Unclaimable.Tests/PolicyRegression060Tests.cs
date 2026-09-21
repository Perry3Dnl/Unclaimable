using Xunit;

namespace Unclaimable.Tests;

public sealed class PolicyRegression060Tests
{
    [Theory]
    [InlineData("supportive", "support", "support")]
    [InlineData("helpful", "help", "support")]
    [InlineData("badminton", "admin", "roles")]
    [InlineData("stafford", "staff", "roles")]
    [InlineData("rooted", "root", "roles")]
    [InlineData("ownership", "owner", "roles")]
    public void PrivilegedAndSupportRootsRejectContainingIdentifiers(
        string value,
        string expectedMatch,
        string expectedCategory)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("apples")]
    [InlineData("nikee")]
    public void BrandAndTechnologyValuesRemainExactUnlessExplicitlyPartial(string value)
    {
        Assert.True(new Checker(new Options()).IsClaimable(value));
    }

    [Theory]
    [InlineData("mysuperadminx", "superadmin", "roles")]
    [InlineData("myloginverificationteamx", "loginverificationteam", "authentication")]
    [InlineData("extracustomersupportx", "customersupport", "support")]
    [InlineData("fuckboyenthusiast", "fuckboy", "profanity")]
    public void ExplicitPartialSafeEntriesStillProtectDangerousCompounds(
        string value,
        string expectedMatch,
        string expectedCategory)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
        Assert.Equal(expectedMatch, result.MatchedValue);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Theory]
    [InlineData("support", "support")]
    [InlineData("help", "support")]
    [InlineData("apple", "technology")]
    [InlineData("nike", "brands")]
    [InlineData("admin", "roles")]
    [InlineData("root", "roles")]
    public void AmbiguousTermsRemainReservedAsExactIdentifiers(string value, string expectedCategory)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(expectedCategory, result.Category);
    }
}
