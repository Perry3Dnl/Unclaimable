namespace Unclaimable;

/// <summary>
/// Optional user-facing validation messages for individual rejection reasons.
/// Leave any property unset to use <see cref="UnclaimableOptions.ValidationMessage"/>
/// or Unclaimable's built-in message for that reason.
/// </summary>
public sealed class UnclaimableValidationMessages
{
    /// <summary>Message for an exact reserved-name match.</summary>
    public string? Reserved { get; set; }

    /// <summary>Message when separators or punctuation are ignored to produce a reserved name.</summary>
    public string? Compact { get; set; }

    /// <summary>Message when a reserved name is embedded inside a larger username.</summary>
    public string? Partial { get; set; }

    /// <summary>Message when leetspeak or symbol substitutions resolve to a reserved name.</summary>
    public string? Obfuscated { get; set; }

    /// <summary>Message when Unicode lookalikes resolve to a reserved name.</summary>
    public string? UnicodeConfusable { get; set; }

    /// <summary>Message when numbers are disallowed by policy.</summary>
    public string? NumbersNotAllowed { get; set; }

    /// <summary>Message when the configured character policy rejects the input.</summary>
    public string? InvalidCharacters { get; set; }

    /// <summary>Message for matches from the opt-in profanity dataset.</summary>
    public string? Profanity { get; set; }
}
