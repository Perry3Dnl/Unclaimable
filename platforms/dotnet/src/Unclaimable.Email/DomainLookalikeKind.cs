namespace Unclaimable.Email;

/// <summary>Describes how an email domain resembles a configured protected domain.</summary>
public enum DomainLookalikeKind
{
    /// <summary>No protected-domain resemblance was detected.</summary>
    None = 0,

    /// <summary>Unicode or common ASCII lookalike normalization resolves to the protected domain.</summary>
    Confusable = 1,

    /// <summary>The domain is within the configured typographical edit distance of a protected domain.</summary>
    Typographical = 2,

    /// <summary>A protected registrant label is reused in another domain name.</summary>
    ProtectedLabelReuse = 3,

    /// <summary>The complete protected domain is embedded as the leading portion of a different domain.</summary>
    EmbeddedProtectedDomain = 4
}
