namespace Unclaimable;

/// <summary>Controls how aggressively Unclaimable applies embedded reserved-name matching rules.</summary>
public enum Strictness
{
    /// <summary>
    /// Uses the more conservative reserved-name behavior. Embedded reserved names are only
    /// rejected when <see cref="Options.PartialMatching"/> is enabled explicitly.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Enables embedded/partial reserved-name matching in addition to the normal checks.
    /// This is the default. It enables partial matching even when <see cref="Options.PartialMatching"/>
    /// is assigned <see langword="false"/> unless <see cref="Rule.PartialMatching"/> is disabled.
    /// </summary>
    Strict = 1
}
