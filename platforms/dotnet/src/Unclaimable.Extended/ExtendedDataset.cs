using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Unclaimable.Extended;

internal static class ExtendedDataset
{
    internal sealed class Entry
    {
        internal Entry(ExtendedCategory category, string categoryName, string value)
        {
            Category = category;
            CategoryName = categoryName;
            Value = value;
        }

        internal ExtendedCategory Category { get; }
        internal string CategoryName { get; }
        internal string Value { get; }
    }

    [DataContract]
    private sealed class Document
    {
        [DataMember(Name = "schema")]
        public int Schema { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; } = string.Empty;

        [DataMember(Name = "values")]
        public string[]? Values { get; set; }
    }

    internal static readonly Lazy<IReadOnlyList<Entry>> Entries =
        new Lazy<IReadOnlyList<Entry>>(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    private static IReadOnlyList<Entry> Load()
    {
        var assembly = typeof(ExtendedDataset).Assembly;
        var serializer = new DataContractJsonSerializer(typeof(Document));
        var entries = new List<Entry>();

        foreach (var resource in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Extended.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"Embedded Extended dataset '{resource}' could not be opened.");
            var document = serializer.ReadObject(stream) as Document
                ?? throw new InvalidOperationException($"Embedded Extended dataset '{resource}' is invalid.");

            if (document.Schema != 1 || !TryResolveCategory(document.Category, out var category))
            {
                throw new InvalidOperationException($"Embedded Extended dataset '{resource}' has an unsupported schema or category.");
            }

            foreach (var value in document.Values ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    entries.Add(new Entry(category, document.Category, value));
                }
            }
        }

        return entries;
    }

    internal static bool TryResolveCategory(string value, out ExtendedCategory category)
    {
        switch (value)
        {
            case "companies": category = ExtendedCategory.Companies; return true;
            case "regionalbrands": category = ExtendedCategory.RegionalBrands; return true;
            case "financialinstitutions": category = ExtendedCategory.FinancialInstitutions; return true;
            case "government": category = ExtendedCategory.Government; return true;
            case "internationalorganizations": category = ExtendedCategory.InternationalOrganizations; return true;
            case "sports": category = ExtendedCategory.Sports; return true;
            case "education": category = ExtendedCategory.Education; return true;
            case "media": category = ExtendedCategory.Media; return true;
            case "transport": category = ExtendedCategory.Transport; return true;
            case "healthcare": category = ExtendedCategory.Healthcare; return true;
            case "historicalfigures": category = ExtendedCategory.HistoricalFigures; return true;
            case "publicfigures": category = ExtendedCategory.PublicFigures; return true;
            case "celebrities": category = ExtendedCategory.Celebrities; return true;
            case "fiction": category = ExtendedCategory.Fiction; return true;
            case "entertainment": category = ExtendedCategory.Entertainment; return true;
            case "professions": category = ExtendedCategory.Professions; return true;
            case "multilingual": category = ExtendedCategory.Multilingual; return true;
            case "slangprofanity": category = ExtendedCategory.SlangProfanity; return true;
            case "crypto": category = ExtendedCategory.Crypto; return true;
            case "platforms": category = ExtendedCategory.Platforms; return true;
            case "geography": category = ExtendedCategory.Geography; return true;
            default: category = default; return false;
        }
    }
}
