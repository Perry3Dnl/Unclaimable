namespace Unclaimable.Email;

/// <summary>Checks email syntax, local-part identity policy, and protected-domain impersonation.</summary>
public interface IEmailChecker
{
    /// <summary>Returns whether an email address is accepted for the specified purpose.</summary>
    bool IsAllowed(string? address, EmailAddressPurpose purpose);

    /// <summary>Checks an email address for the specified purpose.</summary>
    EmailResult Check(string? address, EmailAddressPurpose purpose);

    /// <summary>Checks an externally existing email address.</summary>
    EmailResult CheckExistingAddress(string? address);

    /// <summary>Checks an email address that the application intends to create or issue.</summary>
    EmailResult CheckNewAddress(string? address);
}
