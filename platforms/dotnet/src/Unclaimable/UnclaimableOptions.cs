namespace Unclaimable;

public sealed class UnclaimableOptions
{
    private bool _partialMatching;

    /// <summary>
    /// Controls how aggressively reserved-name rules are applied.
    /// Standard preserves the normal behavior. Strict also enables embedded/partial
    /// reserved-name matching so values such as "admin2" and "old-admin" are rejected.
    /// </summary>
    public UnclaimableStrictness Strictness { get; set; } = UnclaimableStrictness.Standard;

    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// For example, "customer-service" matches "customer service".
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Also reject usernames that contain a reserved value as part of a larger value.
    /// For example, "administrator2" and "old-admin" can match "administrator" and "admin".
    /// This can be enabled directly, and is enabled automatically by Strictness.Strict.
    /// </summary>
    public bool PartialMatching
    {
        get => _partialMatching || Strictness == UnclaimableStrictness.Strict;
        set => _partialMatching = value;
    }

    /// <summary>
    /// Minimum compact reserved-name length eligible for partial matching.
    /// Short values such as "api" are ignored by default because matching them inside
    /// ordinary words can create excessive false positives.
    /// </summary>
    public int PartialMatchMinimumLength { get; set; } = 4;

    /// <summary>
    /// Include the built-in profanity dataset in username matching.
    /// This is intentionally off by default because profanity policies are application-
    /// and culture-specific.
    /// </summary>
    public bool ProfanityMatching { get; set; }

    /// <summary>
    /// Allow the profanity dataset to participate in partial/substring matching when both
    /// ProfanityMatching and PartialMatching are enabled. This is off by default to avoid
    /// false positives such as ordinary words containing a short vulgar fragment.
    /// </summary>
    public bool ProfanityPartialMatching { get; set; }

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
    /// Optional application-wide fallback validation message used by the ASP.NET Core
    /// ClaimableUsername attribute when a username is rejected.
    /// Use {FieldName}, {MatchedValue}, {Category}, {Character}, and {Index} placeholders.
    /// Reason-specific Messages take precedence over this fallback.
    /// An ErrorMessage configured directly on the attribute takes precedence over both.
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Optional reason-specific validation messages. Any unset message falls back to
    /// ValidationMessage, then to Unclaimable's built-in message for that rejection reason.
    /// </summary>
    public UnclaimableValidationMessages Messages { get; } = new UnclaimableValidationMessages();

    /// <summary>
    /// Application-specific names to reserve in addition to the shared dataset.
    /// Tenant names, internal identities, and project-specific terms belong here
    /// rather than in the global data files.
    /// </summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();
}
