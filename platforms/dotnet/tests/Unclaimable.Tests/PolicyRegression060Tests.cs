using System.Text.Json;
using Xunit;

namespace Unclaimable.Tests;

public sealed class PolicyRegression060Tests
{
    public static IEnumerable<object[]> SafeUsernames()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "safe-usernames.json");
        var values = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path))
                     ?? throw new InvalidOperationException("Safe username corpus could not be loaded.");

        return values.Select(value => new object[] { value });
    }

    [Theory]
    [MemberData(nameof(SafeUsernames))]
    public void KnownSafeUsernamesRemainClaimable(string value)
    {
        var result = new Checker(new Options()).Check(value);

        Assert.True(result.IsClaimable, $"Known-safe username '{value}' was rejected as {result.MatchKind} / {result.MatchedValue}.");
    }

    [Theory]
    [InlineData("supportive")]
    [InlineData("helpful")]
    [InlineData("apples")]
    [InlineData("nikee")]
    [InlineData("badminton")]
    [InlineData("rooted")]
    [InlineData("stafford")]
    [InlineData("ownership")]
    public void AmbiguousBuiltInTermsDoNotBecomeGenericSubstringRules(string value)
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
