using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Unclaimable.Extended;

/// <summary>Explicit registration for the separately reviewed supplemental identity vocabulary.</summary>
public static class SupplementalIdentityOptionsExtensions
{
    /// <summary>
    /// Adds supplemental identity entries using Core's exact, whole-identifier matching only.
    /// This method is never invoked by <see cref="ExtendedOptionsExtensions.UseExtendedData"/>.
    /// Existing Core and Extended defaults are unchanged.
    /// </summary>
    public static global::Unclaimable.Options UseSupplementalIdentityData(
        this global::Unclaimable.Options options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        foreach (var group in SupplementalIdentityDataset.Groups.Value)
        {
            foreach (var value in group.Values ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    options.Reserve(value, group.Category, global::Unclaimable.ReservedMatchMode.Exact);
                }
            }
        }

        return options;
    }
}

internal static class SupplementalIdentityDataset
{
    [DataContract]
    internal sealed class Group
    {
        [DataMember(Name = "category")]
        public string Category { get; set; } = string.Empty;

        [DataMember(Name = "values")]
        public string[]? Values { get; set; }
    }

    [DataContract]
    private sealed class Document
    {
        [DataMember(Name = "schema")]
        public int Schema { get; set; }

        [DataMember(Name = "groups")]
        public Group[]? Groups { get; set; }
    }

    internal static readonly Lazy<IReadOnlyList<Group>> Groups =
        new Lazy<IReadOnlyList<Group>>(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    private static IReadOnlyList<Group> Load()
    {
        const string name = "Unclaimable.Extended.Vocabulary.identity.json";
        using var stream = typeof(SupplementalIdentityDataset).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Supplemental identity data resource was not found.");
        var serializer = new DataContractJsonSerializer(typeof(Document));
        var document = serializer.ReadObject(stream) as Document;
        if (document is null || document.Schema != 1 || document.Groups is null)
        {
            throw new InvalidOperationException("Supplemental identity data has an unsupported format.");
        }

        return document.Groups;
    }
}
