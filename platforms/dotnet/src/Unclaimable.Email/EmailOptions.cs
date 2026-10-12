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

    /// <summary>
    /// Gets or sets whether subdomains of configured IssuingDomains are accepted.
    /// True by default to preserve earlier releases; false requires an exact domain match.
    /// Applies when the issuing-domain allowlist is enforced.
    /// </summary>
    public bool AllowIssuingDomainSubdomains { get; set; } = true;

    /// <summary>
    /// Gets or sets whether IssuingDomains is also enforced for externally existing
    /// email addresses (CheckExistingAddress). False by default for compatibility.
    /// </summary>
    public bool EnforceIssuingDomainsForExistingAddresses { get; set; } = false;

    /// <summary>Overrides the per-call email purpose; Auto preserves the existing API behavior.</summary>
    public EmailUsage EmailUsage { get; set; } = EmailUsage.Auto;

    /// <summary>Strict retains earlier behavior; Relaxed permits reserved-looking local parts and suspicious domains for existing addresses.</summary>
    public EmailProtectionLevel EmailProtectionLevel { get; set; } = EmailProtectionLevel.Strict;

    /// <summary>
    /// Optional maximum input length in UTF-16 code units, checked before parsing.
    /// The default has no additional limit; email syntax still enforces its normal bounds.
    /// </summary>
    public int MaximumInputLength { get; set; } = int.MaxValue;

    /// <summary>Preset for accepting existing third-party mailbox identities.</summary>
    public static EmailOptions ForUserRegistration() => new EmailOptions
    {
        EmailUsage = EmailUsage.ExistingAddress,
        EmailProtectionLevel = EmailProtectionLevel.Relaxed
    };

    /// <summary>Preset for restricting existing addresses to an exact organizational domain.</summary>
    public static EmailOptions ForOrganizationEmail(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentException("Domain is required.", nameof(domain));
        var options = new EmailOptions
        {
            EmailUsage = EmailUsage.ExistingAddress,
            EmailProtectionLevel = EmailProtectionLevel.Strict,
            EnforceIssuingDomainsForExistingAddresses = true,
            AllowIssuingDomainSubdomains = false
        };
        options.IssuingDomains.Add(domain);
        return options;
    }

    /// <summary>Preset for email addresses created and issued by this application.</summary>
    public static EmailOptions ForIssuedAddresses() => new EmailOptions
    {
        EmailUsage = EmailUsage.IssuedAddress,
        EmailProtectionLevel = EmailProtectionLevel.Strict
    };

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
