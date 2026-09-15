namespace Unclaimable;

/// <summary>
/// Built-in validation rules. Most structural and matching rules are enabled by default.
/// Number rejection, country-name matching, and popular-city-name matching are disabled by default in 0.7.2.
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
    /// <summary>Reject identifiers containing Unicode decimal digits. Disabled by default in 0.7.2.</summary>
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
    PopularCityNames = 1 << 13
}
