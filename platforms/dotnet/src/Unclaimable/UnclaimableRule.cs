namespace Unclaimable;

/// <summary>
/// Built-in validation rules that can be disabled for applications that need a more permissive policy.
/// All rules are enabled by default unless explicitly disabled.
/// </summary>
[Flags]
public enum UnclaimableRule
{
    /// <summary>No validation rules.</summary>
    None = 0,

    /// <summary>Enforces <see cref="UnclaimableOptions.MinimumLength"/>.</summary>
    MinimumLength = 1 << 0,

    /// <summary>Enforces <see cref="UnclaimableOptions.MaximumLength"/>.</summary>
    MaximumLength = 1 << 1,

    /// <summary>Rejects whitespace unless the character is explicitly allowed by the runtime policy.</summary>
    Whitespace = 1 << 2,

    /// <summary>Rejects characters blocked by the configured character policy.</summary>
    BlockedCharacters = 1 << 3,

    /// <summary>Rejects identifiers that start with a separator.</summary>
    LeadingSeparator = 1 << 4,

    /// <summary>Rejects identifiers that end with a separator.</summary>
    TrailingSeparator = 1 << 5,

    /// <summary>Rejects Unicode decimal digits unless numbers are explicitly allowed.</summary>
    Numbers = 1 << 6,

    /// <summary>Enables matching after separators and punctuation are removed.</summary>
    CompactMatching = 1 << 7,

    /// <summary>Enables matching reserved values embedded inside larger identifiers.</summary>
    PartialMatching = 1 << 8,

    /// <summary>Enables profanity datasets for the selected languages.</summary>
    Profanity = 1 << 9,

    /// <summary>Enables matching of common username obfuscation and leetspeak substitutions.</summary>
    ObfuscationMatching = 1 << 10,

    /// <summary>Enables matching of common Unicode lookalikes and diacritic-based impersonation.</summary>
    UnicodeConfusableMatching = 1 << 11
}
