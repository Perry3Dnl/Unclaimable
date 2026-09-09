namespace Unclaimable;

/// <summary>Checks whether identifier values can be claimed under a captured configuration and live character policy.</summary>
public interface IChecker
{
    /// <summary>
    /// Returns whether the value is rejected. Structural validation failures are included in the reserved result.
    /// A <see langword="null"/> value is accepted; required-field validation remains the application's responsibility.
    /// </summary>
    bool IsReserved(string? value);

    /// <summary>
    /// Returns whether the value is claimable. A <see langword="null"/> value is claimable;
    /// required-field validation remains separate.
    /// </summary>
    bool IsClaimable(string? value);

    /// <summary>Returns the first validation or reserved-name result for the value.</summary>
    Result Check(string? value);

    /// <summary>Returns all collected validation diagnostics and the reserved-name match, if any.</summary>
    DetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
