namespace Unclaimable;

/// <summary>Optional user-facing validation messages for individual rejection reasons.</summary>
public sealed class UnclaimableValidationMessages
{
    public string? Reserved { get; set; }
    public string? Compact { get; set; }
    public string? Partial { get; set; }
    public string? Obfuscated { get; set; }
    public string? UnicodeConfusable { get; set; }
    public string? NumbersNotAllowed { get; set; }
    public string? InvalidCharacters { get; set; }
    public string? TooShort { get; set; }
    public string? TooLong { get; set; }
    public string? BlockedCharacter { get; set; }
    public string? LeadingSeparator { get; set; }
    public string? TrailingSeparator { get; set; }
    public string? Profanity { get; set; }
}
