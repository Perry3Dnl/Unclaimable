using System;
using System.Collections.Generic;

namespace Unclaimable;

/// <summary>
/// Opt-in, non-mutating inspection of an <see cref="Options"/> configuration.
/// Existing checker construction and validation semantics are unchanged.
/// </summary>
public static class PolicyConfigurationInspector
{
    /// <summary>
    /// Returns configuration problems and potentially surprising combinations.
    /// This method does not construct a checker or change any options.
    /// </summary>
    public static IReadOnlyList<string> Inspect(Options options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var problems = new List<string>();
        if (options.MinimumLength < 0)
        {
            problems.Add("MinimumLength must not be negative.");
        }

        if (options.MaximumLength < 1)
        {
            problems.Add("MaximumLength must be at least 1.");
        }

        if (options.MinimumLength > options.MaximumLength)
        {
            problems.Add("MinimumLength must not exceed MaximumLength.");
        }

        if (options.PartialMatchMinimumLength < 1)
        {
            problems.Add("PartialMatchMinimumLength must be at least 1.");
        }

        if (options.RepeatedPatternMinimumLength < 2)
        {
            problems.Add("RepeatedPatternMinimumLength must be at least 2.");
        }

        if (!Enum.IsDefined(typeof(ObfuscationSensitivity), options.ObfuscationSensitivity))
        {
            problems.Add("ObfuscationSensitivity must be a supported value.");
        }

        if (!Enum.IsDefined(typeof(FalsePositiveSensitivity), options.FalsePositiveSensitivity))
        {
            problems.Add("FalsePositiveSensitivity must be a supported value.");
        }

        if (!options.PartialMatching && options.Strictness == Strictness.Strict)
        {
            // The getter re-enables PartialMatching in Strict mode by design;
            // this branch documents future-proof handling without overriding it.
        }

        return problems.AsReadOnly();
    }
}
