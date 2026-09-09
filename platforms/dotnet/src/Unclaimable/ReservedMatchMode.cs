namespace Unclaimable;

/// <summary>Controls how an application-specific reservation participates in reserved-name matching.</summary>
public enum ReservedMatchMode
{
    /// <summary>Uses the checker's configured exact, compact, partial, Unicode-confusable, and obfuscation pipeline.</summary>
    Default = 0,
    /// <summary>Matches only the complete identifier after Unclaimable's exact case and Unicode normalization.</summary>
    Exact = 1
}
