using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class EmailDefaults080Tests
{
    private const Rule V080ProtectedIdentityRules =
        Rule.CountryNames
        | Rule.PopularCityNames
        | Rule.CelebrityNames
        | Rule.Nationalities
        | Rule.Currencies
        | Rule.Religions
        | Rule.Landmarks
        | Rule.Events
        | Rule.Awards
        | Rule.FictionalCharacters
        | Rule.Franchises
        | Rule.Professions
        | Rule.Military;

    [Fact]
    public void EmailLocalPartKeepsThe080ProtectedIdentityDefaults()
    {
        var options = new EmailOptions();

        Assert.Equal(
            V080ProtectedIdentityRules,
            options.LocalPartOptions.EnabledOptionalRules & V080ProtectedIdentityRules);

        Assert.Equal(
            Rule.None,
            options.LocalPartOptions.DisabledRules & V080ProtectedIdentityRules);
    }

    [Theory]
    [InlineData("amsterdam@example.com", MatchKind.PopularCityName)]
    [InlineData("physician@example.com", MatchKind.Exact)]
    public void EmailLocalPartAppliesDefaultProtectedIdentityRules(
        string address,
        MatchKind expectedMatchKind)
    {
        var result = new EmailChecker().CheckExistingAddress(address);

        Assert.False(result.IsAllowed);
        Assert.Equal(EmailFailureKind.ReservedLocalPart, result.FailureKind);
        Assert.NotNull(result.LocalPartResult);
        Assert.Equal(expectedMatchKind, result.LocalPartResult!.MatchKind);
    }

    [Fact]
    public void EmailSpecificSyntaxAdjustmentsRemainSeparateFromIdentityDefaults()
    {
        var options = new EmailOptions();

        Assert.True((options.LocalPartOptions.DisabledRules & Rule.BlockedCharacters) != 0);
        Assert.True((options.LocalPartOptions.DisabledRules & Rule.Whitespace) != 0);
        Assert.Equal(Pattern.None, options.LocalPartOptions.EnabledPatterns);
    }
}
