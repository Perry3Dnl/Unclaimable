namespace Unclaimable;

/// <summary>Controls how an application-specific reservation participates in reserved-name matching.</summary>
public enum ReservedMatchMode
{
    /// <summary>Uses the checker's configured exact, compact, partial, Unicode-confusable, and obfuscation pipeline.</summary>
    Default = 0,
    /// <summary>
    /// Matches only the complete identifier after exact normalization: trim leading/trailing whitespace,
    /// apply Unicode NFKC normalization, then lowercase using invariant casing.
    /// </summary>
    Exact = 1,

    /// <summary>
    /// Matches the complete identifier through exact, compact, Unicode-confusable, and obfuscation matching,
    /// but does not treat the reservation as a generic substring root.
    /// </summary>
    WholeIdentifier = 2
}
