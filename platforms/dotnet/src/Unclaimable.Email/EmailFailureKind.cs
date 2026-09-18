namespace Unclaimable.Email;

/// <summary>Describes the primary reason an email address was rejected.</summary>
public enum EmailFailureKind
{
    /// <summary>No rejection occurred.</summary>
    None = 0,

    /// <summary>The value is not a supported mailbox address form.</summary>
    InvalidFormat = 1,

    /// <summary>The local part is not syntactically valid for an unquoted mailbox address.</summary>
    InvalidLocalPart = 2,

    /// <summary>The domain is not a valid DNS-style internationalized domain name.</summary>
    InvalidDomain = 3,

    /// <summary>The local part was rejected by the configured Unclaimable identifier policy.</summary>
    ReservedLocalPart = 4,

    /// <summary>The domain resembles a configured protected domain closely enough to be suspicious.</summary>
    SuspiciousDomain = 5,

    /// <summary>A newly issued address does not use one of the configured issuing domains.</summary>
    UnapprovedIssuingDomain = 6
}
