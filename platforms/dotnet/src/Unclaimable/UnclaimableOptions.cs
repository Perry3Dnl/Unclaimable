namespace Unclaimable;

public sealed class UnclaimableOptions
{
    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// For example, "customer-service" matches "customer service".
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Also detect common username obfuscation and leetspeak substitutions.
    /// For example, "N1k3" can match the reserved name "nike".
    /// </summary>
    public bool ObfuscationMatching { get; set; } = true;

    /// <summary>
    /// Also detect common Unicode lookalikes and diacritic-based impersonation.
    /// For example, Cyrillic characters in "аpple" can match the reserved name "apple".
    /// </summary>
    public bool UnicodeConfusableMatching { get; set; } = true;

    /// <summary>
    /// Reject input containing characters outside printable ASCII (U+0020 through U+007E).
    /// This is intentionally off by default so applications can support international names.
    /// Application-specific length and punctuation rules should still be validated separately.
    /// </summary>
    public bool AsciiOnly { get; set; }

    /// <summary>
    /// Application-specific names to reserve in addition to the shared dataset.
    /// Tenant names, internal identities, and project-specific terms belong here
    /// rather than in the global data files.
    /// </summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();
}
