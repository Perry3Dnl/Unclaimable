namespace Unclaimable;

public sealed class UnclaimableOptions
{
    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// For example, "customer-service" matches "customer service".
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Also reject usernames that contain a reserved value as part of a larger value.
    /// For example, "administrator2" and "old-admin" can match "administrator" and "admin".
    /// This stricter mode is intentionally off by default to avoid broad false positives.
    /// </summary>
    public bool PartialMatching { get; set; }

    /// <summary>
    /// Minimum compact reserved-name length eligible for partial matching.
    /// Short values such as "api" are ignored by default because matching them inside
    /// ordinary words can create excessive false positives.
    /// </summary>
    public int PartialMatchMinimumLength { get; set; } = 4;

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
    /// Allow Unicode decimal digits in usernames.
    /// Disable this to reject numeric characters as an inexpensive policy check before
    /// reserved-name matching and other more expensive normalization passes.
    /// </summary>
    public bool AllowNumbers { get; set; } = true;

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
