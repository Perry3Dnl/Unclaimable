namespace Unclaimable;

/// <summary>
/// Defines optional user-facing validation messages for individual rejection reasons.
/// </summary>
public sealed class UnclaimableValidationMessages
{
    /// <summary>Gets or sets the message used for an exact reserved-name match.</summary>
    public string? Reserved { get; set; }

    /// <summary>Gets or sets the message used for a compact reserved-name match.</summary>
    public string? Compact { get; set; }

    /// <summary>Gets or sets the message used when a reserved value is embedded in a larger identifier.</summary>
    public string? Partial { get; set; }

    /// <summary>Gets or sets the message used for an obfuscated reserved-name match.</summary>
    public string? Obfuscated { get; set; }

    /// <summary>Gets or sets the message used for a Unicode-confusable reserved-name match.</summary>
    public string? UnicodeConfusable { get; set; }

    /// <summary>Gets or sets the message used when numbers are not allowed.</summary>
    public string? NumbersNotAllowed { get; set; }

    /// <summary>Gets or sets the message used for invalid characters such as non-ASCII input in ASCII-only mode.</summary>
    public string? InvalidCharacters { get; set; }

    /// <summary>Gets or sets the message used when an identifier is shorter than the configured minimum length.</summary>
    public string? TooShort { get; set; }

    /// <summary>Gets or sets the message used when an identifier is longer than the configured maximum length.</summary>
    public string? TooLong { get; set; }

    /// <summary>Gets or sets the message used when an identifier contains a blocked character.</summary>
    public string? BlockedCharacter { get; set; }

    /// <summary>Gets or sets the message used when an identifier starts with a separator.</summary>
    public string? LeadingSeparator { get; set; }

    /// <summary>Gets or sets the message used when an identifier ends with a separator.</summary>
    public string? TrailingSeparator { get; set; }

    /// <summary>Gets or sets the message used when an enabled profanity dataset rejects an identifier.</summary>
    public string? Profanity { get; set; }
}
