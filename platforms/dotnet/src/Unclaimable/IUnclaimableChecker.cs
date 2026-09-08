namespace Unclaimable;

/// <summary>
/// Checks candidate identifiers against the configured Unclaimable policy and reserved-name datasets.
/// </summary>
public interface IUnclaimableChecker
{
    /// <summary>
    /// Determines whether the supplied value is rejected by any configured policy or reserved-name rule.
    /// </summary>
    /// <param name="value">The identifier to evaluate.</param>
    /// <returns><see langword="true"/> when the value is rejected; otherwise, <see langword="false"/>.</returns>
    bool IsReserved(string? value);

    /// <summary>
    /// Determines whether the supplied value can be claimed under the configured policy.
    /// </summary>
    /// <param name="value">The identifier to evaluate.</param>
    /// <returns><see langword="true"/> when the value is allowed; otherwise, <see langword="false"/>.</returns>
    bool IsClaimable(string? value);

    /// <summary>
    /// Evaluates the supplied value and returns the first rejection reason, or an allowed result.
    /// </summary>
    /// <param name="value">The identifier to evaluate.</param>
    /// <returns>The validation result for the supplied value.</returns>
    UnclaimableResult Check(string? value);

    /// <summary>
    /// Evaluates the supplied value and returns all detected policy and reserved-name diagnostics.
    /// </summary>
    /// <param name="value">The identifier to evaluate.</param>
    /// <param name="includeMessages">Whether to include human-readable diagnostic messages.</param>
    /// <returns>A detailed validation result containing every detected diagnostic.</returns>
    UnclaimableDetailedResult CheckDetailed(string? value, bool includeMessages = false);
}
