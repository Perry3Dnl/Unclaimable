namespace Unclaimable.Email;

/// <summary>Identifies how an email address is being accepted by the application.</summary>
public enum EmailAddressPurpose
{
    /// <summary>The address already exists outside the application and is being supplied as an account identity.</summary>
    ExistingAddress = 0,

    /// <summary>The application is creating or issuing the email address.</summary>
    NewAddress = 1
}
