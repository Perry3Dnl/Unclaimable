using System;
using System.Collections.Generic;

namespace Unclaimable.Email;

/// <summary>Configures email-address identity and protected-domain checks.</summary>
public sealed class EmailOptions
{
    /// <summary>Creates options with an email-adapted Unclaimable local-part policy.</summary>
    public EmailOptions()
    {
        ProtectedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IssuingDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        LocalPartOptions = new global::Unclaimable.Options();
        LocalPartOptions.DisableRule(
            global::Unclaimable.Rule.MinimumLength
            | global::Unclaimable.Rule.MaximumLength
            | global::Unclaimable.Rule.Whitespace
            | global::Unclaimable.Rule.BlockedCharacters
            | global::Unclaimable.Rule.LeadingSeparator
            | global::Unclaimable.Rule.TrailingSeparator);

        LocalPartOptions.DisablePattern(
            global::Unclaimable.Pattern.NumericOnly
            | global::Unclaimable.Pattern.Repeated
            | global::Unclaimable.Pattern.SymbolOnly
            | global::Unclaimable.Pattern.AsciiArt
            | global::Unclaimable.Pattern.UppercaseOnly);
    }

    /// <summary>
    /// Gets domains whose identity should be protected against typo, confusable, and label-reuse variants.
    /// Exact protected domains and their subdomains are accepted.
    /// </summary>
    public ISet<string> ProtectedDomains { get; }

    /// <summary>
    /// Gets domains from which this application may issue new email addresses.
    /// Issuing domains are automatically included in protected-domain checks.
    /// </summary>
    public ISet<string> IssuingDomains { get; }

    /// <summary>
    /// Gets the Unclaimable options used to construct the local-part checker.
    /// Email syntax is validated separately, so username-specific length, separator, blocked-character,
    /// whitespace, and shape rules are disabled here by default.
    /// </summary>
    public global::Unclaimable.Options LocalPartOptions { get; }

    /// <summary>Gets or sets whether Unicode and common ASCII lookalike-domain detection is enabled.</summary>
    public bool DetectUnicodeLookalikes { get; set; } = true;

    /// <summary>Gets or sets whether bounded typo and adjacent-transposition detection is enabled.</summary>
    public bool DetectTypographicalLookalikes { get; set; } = true;

    /// <summary>Gets or sets whether protected registrant labels reused in another domain are rejected.</summary>
    public bool DetectProtectedLabelReuse { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum whole-domain typo distance. Supported values are 0 through 2.
    /// The default is 1.
    /// </summary>
    public int MaximumDomainEditDistance { get; set; } = 1;
}
