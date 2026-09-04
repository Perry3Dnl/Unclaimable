namespace Unclaimable;

public sealed class UnclaimableOptions
{
    /// <summary>
    /// Also compare a compact form with separators and punctuation removed.
    /// For example, "customer-service" matches "customer service".
    /// </summary>
    public bool CompactMatching { get; set; } = true;

    /// <summary>
    /// Application-specific names to reserve in addition to the shared dataset.
    /// Brand names belong here rather than in the global data files.
    /// </summary>
    public ICollection<string> AdditionalReserved { get; } = new List<string>();
}
