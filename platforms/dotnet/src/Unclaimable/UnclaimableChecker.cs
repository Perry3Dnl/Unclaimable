using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unclaimable;

public sealed class UnclaimableChecker : IUnclaimableChecker
{
    private sealed class ReservedEntry
    {
        public ReservedEntry(string value, string category)
        {
            Value = value;
            Category = category;
        }

        public string Value { get; }

        public string Category { get; }
    }

    [DataContract]
    private sealed class ReservedListDocument
    {
        [DataMember(Name = "schema")]
        public int Schema { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; } = string.Empty;

        [DataMember(Name = "values")]
        public string[] Values { get; set; } = Array.Empty<string>();
    }

    private static readonly Lazy<IReadOnlyList<ReservedEntry>> BuiltInEntries =
        new Lazy<IReadOnlyList<ReservedEntry>>(LoadBuiltInEntries, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Dictionary<string, ReservedEntry> _exact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _compact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly bool _compactMatching;

    public static UnclaimableChecker Default { get; } = new UnclaimableChecker();

    public UnclaimableChecker()
        : this(new UnclaimableOptions())
    {
    }

    public UnclaimableChecker(UnclaimableOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _compactMatching = options.CompactMatching;

        foreach (var entry in BuiltInEntries.Value)
        {
            Add(entry);
        }

        foreach (var value in options.AdditionalReserved)
        {
            Add(new ReservedEntry(value, "custom"));
        }
    }

    public bool IsReserved(string? value) => Check(value).IsReserved;

    public bool IsClaimable(string? value) => !IsReserved(value);

    public UnclaimableResult Check(string? value)
    {
        var exact = NormalizeExact(value);
        if (exact is null)
        {
            return UnclaimableResult.Allowed(value);
        }

        ReservedEntry? exactMatch;
        if (_exact.TryGetValue(exact, out exactMatch))
        {
            return new UnclaimableResult(
                true,
                value,
                exactMatch.Value,
                exactMatch.Category,
                UnclaimableMatchKind.Exact);
        }

        if (_compactMatching)
        {
            var compact = NormalizeCompact(exact);
            ReservedEntry? compactMatch;
            if (compact.Length > 0 && _compact.TryGetValue(compact, out compactMatch))
            {
                return new UnclaimableResult(
                    true,
                    value,
                    compactMatch.Value,
                    compactMatch.Category,
                    UnclaimableMatchKind.Compact);
            }
        }

        return UnclaimableResult.Allowed(value);
    }

    private void Add(ReservedEntry entry)
    {
        var exact = NormalizeExact(entry.Value);
        if (exact is null)
        {
            return;
        }

        if (!_exact.ContainsKey(exact))
        {
            _exact.Add(exact, entry);
        }

        var compact = NormalizeCompact(exact);
        if (compact.Length > 0 && !_compact.ContainsKey(compact))
        {
            _compact.Add(compact, entry);
        }
    }

    private static string? NormalizeExact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Trim()
            .Normalize(NormalizationForm.FormKC)
            .ToLowerInvariant();
    }

    private static string NormalizeCompact(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
                if (IsLetterOrDigit(category))
                {
                    builder.Append(character);
                    builder.Append(value[index + 1]);
                }

                index++;
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool IsLetterOrDigit(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter
               || category == UnicodeCategory.LowercaseLetter
               || category == UnicodeCategory.TitlecaseLetter
               || category == UnicodeCategory.ModifierLetter
               || category == UnicodeCategory.OtherLetter
               || category == UnicodeCategory.DecimalDigitNumber;
    }

    private static IReadOnlyList<ReservedEntry> LoadBuiltInEntries()
    {
        var assembly = typeof(UnclaimableChecker).Assembly;
        var entries = new List<ReservedEntry>();
        var serializer = new DataContractJsonSerializer(typeof(ReservedListDocument));

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream is null)
                {
                    throw new InvalidOperationException($"Embedded dataset '{resourceName}' could not be opened.");
                }

                var document = serializer.ReadObject(stream) as ReservedListDocument;
                if (document is null)
                {
                    throw new InvalidOperationException($"Embedded dataset '{resourceName}' is invalid.");
                }

                if (document.Schema != 1 || string.IsNullOrWhiteSpace(document.Category))
                {
                    throw new InvalidOperationException($"Embedded dataset '{resourceName}' has an unsupported schema.");
                }

                entries.AddRange(document.Values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => new ReservedEntry(value, document.Category)));
            }
        }

        return entries;
    }
}
