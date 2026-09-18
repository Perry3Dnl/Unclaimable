using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Unclaimable.Extended;

/// <summary>Registers the optional Unclaimable.Extended data with the Core options pipeline.</summary>
public static class ExtendedOptionsExtensions
{
    private static readonly Lazy<IReadOnlyList<ExtendedEntry>> Entries =
        new Lazy<IReadOnlyList<ExtendedEntry>>(LoadEntries, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Registers all Extended datasets. Installing the package alone does not change Core behavior;
    /// this method is the explicit opt-in boundary.
    /// </summary>
    public static global::Unclaimable.Options UseExtendedData(
        this global::Unclaimable.Options options,
        Action<ExtendedOptions>? configure = null)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var extended = new ExtendedOptions();
        configure?.Invoke(extended);

        foreach (var entry in Entries.Value)
        {
            if (!extended.IsEnabled(entry.Category))
            {
                continue;
            }

            options.AddSupplementalWholeIdentifier(entry.Value, entry.Category);
        }

        return options;
    }

    private static IReadOnlyList<ExtendedEntry> LoadEntries()
    {
        var assembly = typeof(ExtendedOptionsExtensions).Assembly;
        var serializer = new DataContractJsonSerializer(typeof(ExtendedDocument));
        var entries = new List<ExtendedEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Extended.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream is null)
                {
                    throw new InvalidOperationException($"Embedded Extended dataset '{resourceName}' could not be opened.");
                }

                var document = serializer.ReadObject(stream) as ExtendedDocument;
                if (document is null
                    || document.Schema != 1
                    || string.IsNullOrWhiteSpace(document.Category))
                {
                    throw new InvalidOperationException($"Embedded Extended dataset '{resourceName}' is invalid.");
                }

                ExtendedOptions.MapCategory(document.Category);

                foreach (var rawValue in document.Values ?? Array.Empty<string>())
                {
                    var value = rawValue?.Trim();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    var duplicateKey =
                        document.Category + "\n" + value.Normalize().ToLowerInvariant();
                    if (!seen.Add(duplicateKey))
                    {
                        continue;
                    }

                    entries.Add(new ExtendedEntry(value, document.Category));
                }
            }
        }

        return entries;
    }

    private sealed class ExtendedEntry
    {
        internal ExtendedEntry(string value, string category)
        {
            Value = value;
            Category = category;
        }

        internal string Value { get; }
        internal string Category { get; }
    }

    [DataContract]
    private sealed class ExtendedDocument
    {
        [DataMember(Name = "schema")]
        public int Schema { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; } = string.Empty;

        [DataMember(Name = "values")]
        public string[]? Values { get; set; }
    }
}
