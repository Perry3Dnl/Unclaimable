namespace Unclaimable;

/// <summary>Optional user-facing validation messages for individual rejection reasons.</summary>
public sealed class ValidationMessages
{
    /// <summary>Creates an empty set of reason-specific message overrides.</summary>
    public ValidationMessages()
    {
    }

    /// <summary>Message for exact reserved-name matches.</summary>
    public string? Reserved { get; set; }
    /// <summary>Message for compact matches.</summary>
    public string? Compact { get; set; }
    /// <summary>Message for partial matches.</summary>
    public string? Partial { get; set; }
    /// <summary>Message for obfuscation matches.</summary>
    public string? Obfuscated { get; set; }
    /// <summary>Message for Unicode-confusable matches.</summary>
    public string? UnicodeConfusable { get; set; }
    /// <summary>Message for number-policy failures.</summary>
    public string? NumbersNotAllowed { get; set; }
    /// <summary>Message for invalid-character or malformed-Unicode failures.</summary>
    public string? InvalidCharacters { get; set; }
    /// <summary>Message for minimum-length failures.</summary>
    public string? TooShort { get; set; }
    /// <summary>Message for maximum-length failures.</summary>
    public string? TooLong { get; set; }
    /// <summary>Message for blocked-character failures.</summary>
    public string? BlockedCharacter { get; set; }
    /// <summary>Message for leading-separator failures.</summary>
    public string? LeadingSeparator { get; set; }
    /// <summary>Message for trailing-separator failures.</summary>
    public string? TrailingSeparator { get; set; }
    /// <summary>Message for profanity matches.</summary>
    public string? Profanity { get; set; }
}
