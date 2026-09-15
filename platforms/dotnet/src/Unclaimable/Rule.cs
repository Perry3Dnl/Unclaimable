namespace Unclaimable;

/// <summary>
/// Built-in validation rules. Most structural and matching rules are enabled by default.
/// Number rejection and all named identity lists are disabled by default unless explicitly enabled.
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
    /// <summary>Reject identifiers containing Unicode decimal digits. Disabled by default in 0.7.2 and later.</summary>
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
    /// <summary>Reject complete identifiers that match a built-in country-name list. Disabled by default.</summary>
    CountryNames = 1 << 12,
    /// <summary>Reject complete identifiers that match a curated list of popular city names. Disabled by default.</summary>
    PopularCityNames = 1 << 13,
    /// <summary>Reject protected celebrity and high-profile public-figure identifiers. Disabled by default.</summary>
    CelebrityNames = 1 << 14,
    /// <summary>Reject nationality and demonym identifiers. Disabled by default.</summary>
    Nationalities = 1 << 15,
    /// <summary>Reject recognized fiat and major digital-currency names and codes. Disabled by default.</summary>
    Currencies = 1 << 16,
    /// <summary>Reject names of major religions, denominations, and widely recognized religious movements. Disabled by default.</summary>
    Religions = 1 << 17,
    /// <summary>Reject globally recognizable physical landmark and monument names. Disabled by default.</summary>
    Landmarks = 1 << 18,
    /// <summary>Reject names of globally recognized recurring events. Disabled by default.</summary>
    Events = 1 << 19,
    /// <summary>Reject major named awards and honors. Disabled by default.</summary>
    Awards = 1 << 20,
    /// <summary>Reject highly recognizable fictional character identities. Disabled by default.</summary>
    FictionalCharacters = 1 << 21,
    /// <summary>Reject major entertainment, game, and media franchise identities. Disabled by default.</summary>
    Franchises = 1 << 22,
    /// <summary>Reject selected high-trust professional identities. Disabled by default.</summary>
    Professions = 1 << 23,
    /// <summary>Reject selected armed-forces branches, ranks, and military identities. Disabled by default.</summary>
    Military = 1 << 24
}
