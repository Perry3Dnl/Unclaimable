namespace Unclaimable;

/// <summary>Controls how aggressively Unclaimable applies embedded reserved-name matching rules.</summary>
public enum Strictness
{
    /// <summary>
    /// Uses the more conservative reserved-name behavior. Embedded reserved names are only
    /// considered when <see cref="Options.PartialMatching"/> is enabled explicitly.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Enables the partial-matching capability in addition to the normal checks.
    /// This is the default. Since 0.6.0, built-in dataset entries must also be explicitly
    /// eligible for partial matching; strict mode does not make every built-in value a substring rule.
    /// The capability remains enabled even when <see cref="Options.PartialMatching"/> is assigned
    /// <see langword="false"/> unless <see cref="Rule.PartialMatching"/> is disabled.
    /// </summary>
    Strict = 1
}
