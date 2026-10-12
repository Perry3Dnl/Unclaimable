namespace Unclaimable.Email;

/// <summary>Controls how suspicious existing email identities are enforced.</summary>
public enum EmailProtectionLevel
{
    /// <summary>Reject reserved local parts and suspicious domains as in earlier releases.</summary>
    Strict = 0,
    /// <summary>Keep diagnostics but do not automatically reject reserved local parts or suspicious domains for existing addresses.</summary>
    Relaxed = 1
}
