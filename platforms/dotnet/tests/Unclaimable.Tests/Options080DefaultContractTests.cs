using Xunit;

namespace Unclaimable.Tests;

/// <summary>
/// Frozen compatibility contract for the public Options defaults established by version 0.8.0.
/// Later releases may add new options, but these baseline defaults must not be changed.
/// </summary>
public sealed class Options080DefaultContractTests
{
    private const Pattern V080EnabledPatterns =
        Pattern.NumericOnly
        | Pattern.Repeated
        | Pattern.SymbolOnly
        | Pattern.AsciiArt;

    private const Rule V080EnabledOptionalRules =
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
    public void Version080DefaultsRemainStable()
    {
        var options = new Options();

        Assert.Equal(Strictness.Strict, options.Strictness);
        Assert.Equal(Rule.Numbers, options.DisabledRules);

        Assert.Single(options.Languages);
        Assert.Contains(Language.English, options.Languages);

        Assert.Equal(3, options.MinimumLength);
        Assert.Equal(32, options.MaximumLength);
        Assert.True(options.CompactMatching);
        Assert.True(options.ConsistentCompactMatching);
        Assert.True(options.PartialMatching);
        Assert.Equal(4, options.PartialMatchMinimumLength);
        Assert.True(options.ProfanityMatching);
        Assert.False(options.ProfanityPartialMatching);
        Assert.True(options.ObfuscationMatching);
        Assert.True(options.UnicodeConfusableMatching);
        Assert.False(options.AllowNumbers);
        Assert.False(options.AsciiOnly);
        Assert.True(options.RejectInvisibleOnlyIdentifiers);
        Assert.True(options.RejectControlCharacters);
        Assert.True(options.RejectFormatCharacters);

        Assert.Equal(V080EnabledPatterns, options.EnabledPatterns);
        Assert.Equal(6, options.RepeatedPatternMinimumLength);
        Assert.Equal(V080EnabledOptionalRules, options.EnabledOptionalRules);

        Assert.Empty(options.DisabledCategories);
        Assert.Empty(options.AllowedIdentifiers);
        Assert.Empty(options.AdditionalReserved);
        Assert.Empty(options.ConfiguredBlockedCharacters);

        Assert.Null(options.ValidationMessage);
        Assert.All(
            typeof(ValidationMessages).GetProperties(),
            property => Assert.Null(property.GetValue(options.Messages)));
    }
}
