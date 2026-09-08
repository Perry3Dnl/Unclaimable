using Xunit;

namespace Unclaimable.Tests;

public sealed class SchemaV2CoverageTests
{
    [Fact]
    public void SafeProfanityCompoundMatchesInsideLargerValueByDefault()
    {
        var checker = new Checker(new Options());

        var result = checker.Check("fuckboyenthusiast");

        Assert.True(result.IsReserved);
        Assert.Equal("fuckboy", result.MatchedValue);
        Assert.Equal("profanity", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void AmbiguousShortProfanityDoesNotBecomeGenericSubstringRule()
    {
        var checker = new Checker(new Options());

        Assert.True(checker.IsReserved("ass"));
        Assert.True(checker.IsClaimable("classic"));
        Assert.True(checker.IsClaimable("hole"));
    }

    [Fact]
    public void SchemaV2CombinationCreatesConcreteGlobalEntry()
    {
        var checker = new Checker(new Options());

        var result = checker.Check("loginverificationteam");

        Assert.True(result.IsReserved);
        Assert.Equal("loginverificationteam", result.MatchedValue);
        Assert.Equal("authentication", result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
    }

    [Fact]
    public void ExpandedGlobalEntryParticipatesInStrictPartialMatching()
    {
        var checker = new Checker(new Options());

        var result = checker.Check("myloginverificationteamx");

        Assert.True(result.IsReserved);
        Assert.Equal("loginverificationteam", result.MatchedValue);
        Assert.Equal("authentication", result.Category);
        Assert.Equal(MatchKind.Partial, result.MatchKind);
    }

    [Fact]
    public void LocalizedExpansionIsLoadedOnlyWhenLanguageIsEnabled()
    {
        const string value = "ledenprofielbalie";
        var englishOnly = new Checker(new Options());
        var dutch = new Checker(new Options().AddLanguage(Language.Dutch));

        Assert.True(englishOnly.IsClaimable(value));

        var result = dutch.Check(value);
        Assert.True(result.IsReserved);
        Assert.Equal(value, result.MatchedValue);
        Assert.Equal("identity", result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
    }
}
