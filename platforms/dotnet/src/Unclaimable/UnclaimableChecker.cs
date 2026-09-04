using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unclaimable;

public sealed class UnclaimableChecker : IUnclaimableChecker
{
    private sealed record ReservedEntry(string Value, string Category);

    private sealed class ReservedListDocument
    {
        [JsonPropertyName("schema")]
        public int Schema { get; init; }

        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        [JsonPropertyName("values")]
        public string[] Values { get; init; } = [];
    }

    private static readonly Lazy<IReadOnlyList<ReservedEntry>> BuiltInEntries =
        new(LoadBuiltInEntries, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Dictionary<string, ReservedEntry> _exact = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _compact = new(StringComparer.Ordinal);
    private readonly bool _compactMatching;

    public static UnclaimableChecker Default { get; } = new();

    public UnclaimableChecker()
        : this(new UnclaimableOptions())
    {
    }

    public UnclaimableChecker(UnclaimableOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
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

        if (_exact.TryGetValue(exact, out var exactMatch))
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
            if (compact.Length > 0 && _compact.TryGetValue(compact, out var compactMatch))
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

        _exact.TryAdd(exact, entry);

        var compact = NormalizeCompact(exact);
        if (compact.Length > 0)
        {
            _compact.TryAdd(compact, entry);
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

        foreach (var rune in value.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                builder.Append(rune.ToString());
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<ReservedEntry> LoadBuiltInEntries()
    {
        var assembly = typeof(UnclaimableChecker).Assembly;
        var entries = new List<ReservedEntry>();

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                               ?? throw new InvalidOperationException($"Embedded dataset '{resourceName}' could not be opened.");

            var document = JsonSerializer.Deserialize<ReservedListDocument>(stream)
                           ?? throw new InvalidOperationException($"Embedded dataset '{resourceName}' is invalid.");

            if (document.Schema != 1 || string.IsNullOrWhiteSpace(document.Category))
            {
                throw new InvalidOperationException($"Embedded dataset '{resourceName}' has an unsupported schema.");
            }

            entries.AddRange(document.Values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => new ReservedEntry(value, document.Category)));
        }

        return entries;
    }
}
