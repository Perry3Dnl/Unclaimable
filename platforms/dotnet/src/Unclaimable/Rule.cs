namespace Unclaimable;

/// <summary>
/// Built-in validation rules. All rules except <see cref="Numbers"/> are enabled by default in 0.8.0.
/// Rules can be disabled explicitly through <see cref="Options.DisableRule(Rule)"/>.
/// </summary>
[Flags]
public enum Rule
{
    /// <summary>No rule flag.</summary>
    None = 0,
    /// <summary>Minimum-length validation.</summary>
    MinimumLength = 1 << 0,
    /// <summary>Maximum-length validation.</summary>
    MaximumLength = 1 << 1,
    /// <summary>Whitespace validation.</summary>
    Whitespace = 1 << 2,
    /// <summary>Character-policy validation.</summary>
    BlockedCharacters = 1 << 3,
    /// <summary>Leading-separator validation.</summary>
    LeadingSeparator = 1 << 4,
    /// <summary>Trailing-separator validation.</summary>
    TrailingSeparator = 1 << 5,
    /// <summary>Reject identifiers containing Unicode decimal digits.</summary>
    Numbers = 1 << 6,
    /// <summary>Compact reserved-name matching.</summary>
    CompactMatching = 1 << 7,
    /// <summary>Partial reserved-name matching.</summary>
    PartialMatching = 1 << 8,
    /// <summary>Profanity dataset matching.</summary>
    Profanity = 1 << 9,
    /// <summary>Obfuscation and leetspeak matching.</summary>
    ObfuscationMatching = 1 << 10,
    /// <summary>Unicode-confusable matching.</summary>
    UnicodeConfusableMatching = 1 << 11,
    /// <summary>Reject complete identifiers that match a built-in country-name list.</summary>
    CountryNames = 1 << 12,
    /// <summary>Reject complete identifiers that match a curated list of popular city names.</summary>
    PopularCityNames = 1 << 13,
    /// <summary>Reject protected celebrity and high-profile public-figure identifiers.</summary>
    CelebrityNames = 1 << 14,
    /// <summary>Reject nationality and demonym identifiers.</summary>
    Nationalities = 1 << 15,
    /// <summary>Reject recognized fiat and major digital-currency names and codes.</summary>
    Currencies = 1 << 16,
    /// <summary>Reject names of major religions, denominations, and widely recognized religious movements.</summary>
    Religions = 1 << 17,
    /// <summary>Reject globally recognizable physical landmark and monument names.</summary>
    Landmarks = 1 << 18,
    /// <summary>Reject names of globally recognized recurring events.</summary>
    Events = 1 << 19,
    /// <summary>Reject major named awards and honors.</summary>
    Awards = 1 << 20,
    /// <summary>Reject highly recognizable fictional character identities.</summary>
    FictionalCharacters = 1 << 21,
    /// <summary>Reject major entertainment, game, and media franchise identities.</summary>
    Franchises = 1 << 22,
    /// <summary>Reject selected high-trust professional identities.</summary>
    Professions = 1 << 23,
    /// <summary>Reject selected armed-forces branches, ranks, and military identities.</summary>
    Military = 1 << 24
}
