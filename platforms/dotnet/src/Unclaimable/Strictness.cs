namespace Unclaimable;

/// <summary>
/// Controls how aggressively Unclaimable applies embedded reserved-name matching rules.
/// </summary>
public enum Strictness
{
    /// <summary>
    /// Uses the more conservative reserved-name behavior. Embedded reserved names are only
    /// rejected when <see cref="Options.PartialMatching"/> is enabled explicitly.
    /// Use this when an application intentionally prefers fewer substring matches.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Enables embedded/partial reserved-name matching in addition to the normal checks.
    /// This is the default through <see cref="Options"/>. Values such as
    /// "supportive", "apples", and "nikee" can therefore resolve to protected values.
    /// </summary>
    Strict = 1
}
