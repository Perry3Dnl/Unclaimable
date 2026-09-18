namespace Unclaimable.Email;

/// <summary>Describes the result of an Unclaimable email-address check.</summary>
public sealed class EmailResult
{
    internal EmailResult(
        string? address,
        EmailAddressPurpose purpose,
        string? localPart,
        string? domain,
        EmailFailureKind failureKind,
        global::Unclaimable.Result? localPartResult,
        DomainLookalikeKind domainLookalikeKind,
        string? matchedProtectedDomain)
    {
        Address = address;
        Purpose = purpose;
        LocalPart = localPart;
        Domain = domain;
        FailureKind = failureKind;
        LocalPartResult = localPartResult;
        DomainLookalikeKind = domainLookalikeKind;
        MatchedProtectedDomain = matchedProtectedDomain;
    }

    /// <summary>Gets the original address supplied to the checker.</summary>
    public string? Address { get; }

    /// <summary>Gets the purpose used for this check.</summary>
    public EmailAddressPurpose Purpose { get; }

    /// <summary>Gets the parsed local part when parsing reached that stage.</summary>
    public string? LocalPart { get; }

    /// <summary>Gets the normalized ASCII domain when domain parsing succeeded.</summary>
    public string? Domain { get; }

    /// <summary>Gets the primary rejection reason.</summary>
    public EmailFailureKind FailureKind { get; }

    /// <summary>Gets whether the address passed all checks for the requested purpose.</summary>
    public bool IsAllowed => FailureKind == EmailFailureKind.None;

    /// <summary>Gets the Unclaimable local-part result when email parsing succeeded.</summary>
    public global::Unclaimable.Result? LocalPartResult { get; }

    /// <summary>Gets the detected protected-domain resemblance, if any.</summary>
    public DomainLookalikeKind DomainLookalikeKind { get; }

    /// <summary>Gets whether the domain was classified as a protected-domain lookalike.</summary>
    public bool IsSuspiciousDomain => DomainLookalikeKind != DomainLookalikeKind.None;

    /// <summary>Gets the configured protected domain associated with the lookalike result, if any.</summary>
    public string? MatchedProtectedDomain { get; }
}
