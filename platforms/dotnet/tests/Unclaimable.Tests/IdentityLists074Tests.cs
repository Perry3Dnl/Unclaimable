using Xunit;

namespace Unclaimable.Tests;

public sealed class IdentityLists074Tests
{
    public static IEnumerable<object[]> RepresentativeCases()
    {
        yield return new object[] { Rule.Nationalities, "dutch", "nationality" };
        yield return new object[] { Rule.Currencies, "eur", "currency" };
        yield return new object[] { Rule.Religions, "buddhism", "religion" };
        yield return new object[] { Rule.Landmarks, "eiffeltower", "landmark" };
        yield return new object[] { Rule.Events, "eurovision", "event" };
        yield return new object[] { Rule.Awards, "nobelprize", "award" };
        yield return new object[] { Rule.FictionalCharacters, "darthvader", "fictionalcharacter" };
        yield return new object[] { Rule.Franchises, "starwars", "franchise" };
        yield return new object[] { Rule.Professions, "physician", "profession" };
        yield return new object[] { Rule.Military, "airforce", "military" };
    }

    [Fact]
    public void NewIdentityListsAreDisabledByDefault()
    {
        const Rule v074Rules =
            Rule.Nationalities
            | Rule.Currencies
            | Rule.Religions
            | Rule.Landmarks
            | Rule.Events
            | Rule.Awards
            | Rule.FictionalCharacters
            | Rule.Franchises
            | Rule.Professions
            | Rule.Military;

        var options = new Options();

        Assert.Equal(Rule.None, options.EnabledOptionalRules & v074Rules);
    }

    [Theory]
    [MemberData(nameof(RepresentativeCases))]
    public void EachIdentityListCanBeEnabledIndependently(Rule rule, string value, string expectedCategory)
    {
        var result = new Checker(CreateIdentityOnlyOptions(rule)).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Fact]
    public void NationalitiesAreSeparateFromCountries()
    {
        var checker = new Checker(CreateIdentityOnlyOptions(Rule.Nationalities));

        Assert.Equal("nationality", checker.Check("dutch").Category);
        Assert.True(checker.IsClaimable("netherlands"));
    }

    [Fact]
    public void CurrenciesDoNotAbsorbGeneralFinanceTerms()
    {
        var checker = new Checker(CreateIdentityOnlyOptions(Rule.Currencies));

        Assert.Equal("currency", checker.Check("euro").Category);
        Assert.True(checker.IsClaimable("billing"));
        Assert.True(checker.IsClaimable("treasury"));
    }

    [Fact]
    public void ReligionsDoNotAbsorbReligiousTitles()
    {
        var checker = new Checker(CreateIdentityOnlyOptions(Rule.Religions));

        Assert.Equal("religion", checker.Check("islam").Category);
        Assert.True(checker.IsClaimable("priest"));
        Assert.True(checker.IsClaimable("imam"));
    }

    [Fact]
    public void LandmarksDoNotAbsorbCities()
    {
        var checker = new Checker(CreateIdentityOnlyOptions(Rule.Landmarks));

        Assert.Equal("landmark", checker.Check("tajmahal").Category);
        Assert.True(checker.IsClaimable("paris"));
        Assert.True(checker.IsClaimable("amsterdam"));
    }

    [Fact]
    public void EventsAndAwardsStaySeparate()
    {
        var events = new Checker(CreateIdentityOnlyOptions(Rule.Events));
        var awards = new Checker(CreateIdentityOnlyOptions(Rule.Awards));

        Assert.Equal("event", events.Check("superbowl").Category);
        Assert.True(events.IsClaimable("oscars"));
        Assert.Equal("award", awards.Check("oscars").Category);
        Assert.True(awards.IsClaimable("superbowl"));
    }

    [Fact]
    public void FictionalCharactersAndFranchisesStaySeparate()
    {
        var characters = new Checker(CreateIdentityOnlyOptions(Rule.FictionalCharacters));
        var franchises = new Checker(CreateIdentityOnlyOptions(Rule.Franchises));

        Assert.Equal("fictionalcharacter", characters.Check("darthvader").Category);
        Assert.True(characters.IsClaimable("starwars"));
        Assert.Equal("franchise", franchises.Check("starwars").Category);
        Assert.True(franchises.IsClaimable("darthvader"));
    }

    [Fact]
    public void ProfessionsAndMilitaryStaySeparate()
    {
        var professions = new Checker(CreateIdentityOnlyOptions(Rule.Professions));
        var military = new Checker(CreateIdentityOnlyOptions(Rule.Military));

        Assert.Equal("profession", professions.Check("doctor").Category);
        Assert.True(professions.IsClaimable("general"));
        Assert.Equal("military", military.Check("general").Category);
        Assert.True(military.IsClaimable("doctor"));
    }

    [Theory]
    [InlineData(Rule.Nationalities, "americanfootball")]
    [InlineData(Rule.Currencies, "dollarstore")]
    [InlineData(Rule.Religions, "buddhismhistory")]
    [InlineData(Rule.Landmarks, "eiffeltowerview")]
    [InlineData(Rule.Events, "superbowlparty")]
    [InlineData(Rule.Awards, "oscarwinner")]
    [InlineData(Rule.FictionalCharacters, "batmanfan")]
    [InlineData(Rule.Franchises, "starwarsfan")]
    [InlineData(Rule.Professions, "doctorwho")]
    [InlineData(Rule.Military, "generallist")]
    public void IdentityListsDoNotBecomeSubstringRules(Rule rule, string value)
    {
        Assert.True(new Checker(CreateIdentityOnlyOptions(rule)).IsClaimable(value));
    }

    [Theory]
    [InlineData(Rule.Landmarks, "Eiffel Tower", "landmark")]
    [InlineData(Rule.FictionalCharacters, "Darth-Vader", "fictionalcharacter")]
    [InlineData(Rule.Franchises, "Harry Potter", "franchise")]
    [InlineData(Rule.Military, "Air Force", "military")]
    public void IdentityListsUseWholeIdentifierCompactMatching(Rule rule, string value, string expectedCategory)
    {
        var options = CreateIdentityOnlyOptions(rule)
            .DisableRule(Rule.Whitespace | Rule.BlockedCharacters);

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Compact, result.MatchKind);
        Assert.Equal(expectedCategory, result.Category);
    }

    [Fact]
    public void IdentityListsUseObfuscationMatchingWithoutPartialExpansion()
    {
        var checker = new Checker(CreateIdentityOnlyOptions(Rule.Professions));
        var result = checker.Check("d0ctor");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Obfuscated, result.MatchKind);
        Assert.Equal("doctor", result.MatchedValue);
        Assert.Equal("profession", result.Category);
    }

    [Fact]
    public void AllowedIdentifiersDoNotBypassEnabledIdentityLists()
    {
        var options = CreateIdentityOnlyOptions(Rule.Nationalities);
        options.AllowedIdentifiers.Add("dutch");

        var result = new Checker(options).Check("dutch");

        Assert.True(result.IsReserved);
        Assert.Equal("nationality", result.Category);
    }

    [Fact]
    public void MultipleIdentityListsCanBeEnabledTogether()
    {
        var options = CreateIdentityOnlyOptions(
            Rule.Nationalities | Rule.Currencies | Rule.Events | Rule.FictionalCharacters);
        var checker = new Checker(options);

        Assert.Equal("nationality", checker.Check("german").Category);
        Assert.Equal("currency", checker.Check("bitcoin").Category);
        Assert.Equal("event", checker.Check("olympics").Category);
        Assert.Equal("fictionalcharacter", checker.Check("batman").Category);
    }

    private static Options CreateIdentityOnlyOptions(Rule enabledRules)
    {
        var options = new Options().EnableRule(enabledRules);

        foreach (var category in Enum.GetValues<Category>())
        {
            options.DisableCategory(category);
        }

        return options;
    }
}
