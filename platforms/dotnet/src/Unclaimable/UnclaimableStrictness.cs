namespace Unclaimable;

/// <summary>
/// Controls how aggressively Unclaimable applies reserved-name matching rules.
/// </summary>
public enum UnclaimableStrictness
{
    /// <summary>
    /// Preserves the normal matching behavior. Embedded reserved names are only
    /// rejected when <see cref="UnclaimableOptions.PartialMatching"/> is enabled explicitly.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Enables embedded/partial reserved-name matching in addition to the normal checks.
    /// For example, "admin2", "old-admin", and "admin-old" are rejected because they
    /// contain the reserved value "admin".
    /// </summary>
    Strict = 1
}
