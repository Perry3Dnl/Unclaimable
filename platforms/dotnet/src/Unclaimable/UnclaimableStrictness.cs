namespace Unclaimable;

/// <summary>
/// Controls how aggressively Unclaimable applies reserved-name matching rules.
/// </summary>
public enum UnclaimableStrictness
{
    /// <summary>
    /// Uses exact and compact matching only. This is useful for applications that
    /// prefer fewer false positives and do not need impersonation detection.
    /// </summary>
    Basic = 0,

    /// <summary>
    /// Uses exact, compact, common obfuscation, and selected Unicode-confusable matching.
    /// Embedded reserved names remain allowed unless partial matching is enabled explicitly.
    /// </summary>
    Standard = 1,

    /// <summary>
    /// Enables embedded/partial reserved-name matching in addition to the normal checks.
    /// For example, "admin2", "old-admin", and "admin-old" are rejected because they
    /// contain the reserved value "admin".
    /// </summary>
    Strict = 2
}
