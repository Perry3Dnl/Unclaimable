namespace Unclaimable.Email;

/// <summary>Controls which address-purpose policy to apply.</summary>
public enum EmailUsage
{
    /// <summary>Use the purpose supplied by the caller (backward-compatible).</summary>
    Auto = 0,
    /// <summary>Apply externally-owned address behavior.</summary>
    ExistingAddress = 1,
    /// <summary>Apply application-issued address behavior.</summary>
    IssuedAddress = 2
}
