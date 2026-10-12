using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Unclaimable.Email;

/// <summary>
/// Offline PSL resolver. Uses the longest matching rule, wildcard rules, and
/// exception rules from the embedded upstream snapshot. Unknown TLDs use the
/// PSL prevailing "*" rule. Data is loaded once and shared by all checkers.
/// </summary>
internal static class PublicSuffixResolver
{
    private static readonly Lazy<HashSet<string>> Rules =
        new Lazy<HashSet<string>>(LoadRules, true);

    private static HashSet<string> LoadRules()
    {
        var assembly = typeof(PublicSuffixResolver).GetTypeInfo().Assembly;
        using (var stream = assembly.GetManifestResourceStream(
            "Unclaimable.Email.public_suffix_list.dat"))
        {
            if (stream == null)
                throw new InvalidOperationException("Embedded Public Suffix List is missing.");
            using (var reader = new StreamReader(stream))
            {
                var rules = new HashSet<string>(StringComparer.Ordinal);
                string? line;
                var idn = new System.Globalization.IdnMapping { UseStd3AsciiRules = true };
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                        continue;

                    var prefix = "";
                    if (line.StartsWith("!", StringComparison.Ordinal))
                    {
                        prefix = "!";
                        line = line.Substring(1);
                    }
                    else if (line.StartsWith("*.", StringComparison.Ordinal))
                    {
                        prefix = "*.";
                        line = line.Substring(2);
                    }
                    try
                    {
                        rules.Add(prefix + idn.GetAscii(line).ToLowerInvariant());
                    }
                    catch (ArgumentException)
                    {
                        // Older .NET IDNA implementations may reject Unicode rules
                        // that are valid in newer IDNA revisions.
                        // Keep loading the remainder of the pinned dataset.
                    }
                }
                return rules;
            }
        }
    }

    internal static string GetRegistrantLabel(string domain)
    {
        var labels = domain.Split('.');
        if (labels.Length < 2)
            return labels[0];

        var rules = Rules.Value;
        var bestLength = 1; // Prevailing "*" rule.
        var exceptionLength = 0;
        for (var i = 0; i < labels.Length; i++)
        {
            var suffix = string.Join(".", labels, i, labels.Length - i);
            var suffixLength = labels.Length - i;

            if (rules.Contains("!" + suffix))
                exceptionLength = Math.Max(exceptionLength, suffixLength);
            if (rules.Contains(suffix))
                bestLength = Math.Max(bestLength, suffixLength);

            if (i + 1 < labels.Length)
            {
                var parent = string.Join(".", labels, i + 1, suffixLength - 1);
                if (rules.Contains("*." + parent))
                    bestLength = Math.Max(bestLength, suffixLength);
            }
        }

        if (exceptionLength > 0)
            bestLength = exceptionLength - 1;

        return labels[Math.Max(0, labels.Length - bestLength - 1)];
    }
}
