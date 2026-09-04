using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unclaimable;

public sealed class UnclaimableChecker : IUnclaimableChecker
{
    private const int MaxObfuscationCandidates = 32;

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
    private readonly bool _obfuscationMatching;

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
        _obfuscationMatching = options.ObfuscationMatching;

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
            return CreateReservedResult(value, exactMatch, UnclaimableMatchKind.Exact);
        }

        if (_compactMatching)
        {
            var compact = NormalizeCompact(exact);
            ReservedEntry? compactMatch;
            if (compact.Length > 0 && _compact.TryGetValue(compact, out compactMatch))
            {
                return CreateReservedResult(value, compactMatch, UnclaimableMatchKind.Compact);
            }
        }

        if (_obfuscationMatching)
        {
            ReservedEntry? obfuscatedMatch;
            if (TryMatchObfuscated(exact, out obfuscatedMatch))
            {
                return CreateReservedResult(value, obfuscatedMatch!, UnclaimableMatchKind.Obfuscated);
            }
        }

        return UnclaimableResult.Allowed(value);
    }

    private static UnclaimableResult CreateReservedResult(
        string? input,
        ReservedEntry match,
        UnclaimableMatchKind matchKind)
    {
        return new UnclaimableResult(
            true,
            input,
            match.Value,
            match.Category,
            matchKind);
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

    private bool TryMatchObfuscated(string value, out ReservedEntry? match)
    {
        var candidates = new List<string> { string.Empty };
        var usedSubstitution = false;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            string[]? substitutions;

            if (TryGetObfuscationSubstitutions(character, out substitutions))
            {
                usedSubstitution = true;
                candidates = ExpandCandidates(candidates, substitutions!);
                continue;
            }

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
                if (IsLetterOrDigit(category))
                {
                    AppendToCandidates(candidates, new string(new[] { character, value[index + 1] }));
                }

                index++;
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                AppendToCandidates(candidates, character.ToString());
            }
        }

        if (!usedSubstitution)
        {
            match = null;
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Length > 0 && _compact.TryGetValue(candidate, out match))
            {
                return true;
            }
        }

        match = null;
        return false;
    }

    private static List<string> ExpandCandidates(List<string> candidates, string[] substitutions)
    {
        var expanded = new List<string>(Math.Min(
            MaxObfuscationCandidates,
            candidates.Count * substitutions.Length));

        foreach (var candidate in candidates)
        {
            foreach (var substitution in substitutions)
            {
                if (expanded.Count >= MaxObfuscationCandidates)
                {
                    return expanded;
                }

                expanded.Add(candidate + substitution);
            }
        }

        return expanded;
    }

    private static void AppendToCandidates(List<string> candidates, string value)
    {
        for (var index = 0; index < candidates.Count; index++)
        {
            candidates[index] += value;
        }
    }

    private static bool TryGetObfuscationSubstitutions(char character, out string[]? substitutions)
    {
        switch (character)
        {
            case '0':
                substitutions = new[] { "o" };
                return true;
            case '1':
                substitutions = new[] { "i", "l" };
                return true;
            case '2':
                substitutions = new[] { "z" };
                return true;
            case '3':
                substitutions = new[] { "e" };
                return true;
            case '4':
                substitutions = new[] { "a" };
                return true;
            case '5':
                substitutions = new[] { "s" };
                return true;
            case '6':
            case '9':
                substitutions = new[] { "g" };
                return true;
            case '7':
                substitutions = new[] { "t" };
                return true;
            case '8':
                substitutions = new[] { "b" };
                return true;
            case '@':
                substitutions = new[] { "a" };
                return true;
            case '$':
                substitutions = new[] { "s" };
                return true;
            case '!':
            case '|':
                substitutions = new[] { "i", "l" };
                return true;
            case '+':
                substitutions = new[] { "t" };
                return true;
            default:
                substitutions = null;
                return false;
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
