namespace Unclaimable;

public sealed class UnclaimableOptions
{
    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// For example, "customer-service" matches "customer service".
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Also detect common username obfuscation and leetspeak substitutions.
    /// For example, "N1k3" can match the reserved name "nike".
    /// </summary>
    public bool ObfuscationMatching { get; set; } = true;

    /// <summary>
    /// Application-specific names to reserve in addition to the shared dataset.
    /// Tenant names, internal identities, and project-specific terms belong here
    /// rather than in the global data files.
    /// </summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();
}
