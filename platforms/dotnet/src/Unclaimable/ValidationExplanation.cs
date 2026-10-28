using System;
using System.Collections.Generic;

namespace Unclaimable;

/// <summary>
/// Additive, read-only conveniences for consuming detailed validation diagnostics.
/// No additional matching pass or rule change is performed.
/// </summary>
public static class ValidationExplanation
{
    /// <summary>Gets a stable, developer-oriented description of an existing diagnostic.</summary>
    public static string Describe(Diagnostic diagnostic)
    {
        if (diagnostic is null)
        {
            throw new ArgumentNullException(nameof(diagnostic));
        }

        var reason = diagnostic.Kind.ToString();
        if (!string.IsNullOrEmpty(diagnostic.Category))
        {
            reason += " (category: " + diagnostic.Category + ")";
        }

        if (diagnostic.OriginalMatchStartIndex.HasValue && diagnostic.OriginalMatchLength.HasValue)
        {
            reason += " at original UTF-16 offset " + diagnostic.OriginalMatchStartIndex.Value
                + ", length " + diagnostic.OriginalMatchLength.Value;
        }
        else if (diagnostic.OffendingCharacterIndex.HasValue)
        {
            reason += " at original UTF-16 offset " + diagnostic.OffendingCharacterIndex.Value;
        }

        return reason;
    }

    /// <summary>Describes diagnostics already collected by <see cref="Checker.CheckDetailed"/>.</summary>
    public static IReadOnlyList<string> Describe(DetailedResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        var descriptions = new List<string>(result.Diagnostics.Count);
        foreach (var diagnostic in result.Diagnostics)
        {
            descriptions.Add(Describe(diagnostic));
        }

        return descriptions.AsReadOnly();
    }
}
