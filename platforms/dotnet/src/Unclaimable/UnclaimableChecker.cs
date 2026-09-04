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
    private readonly bool _unicodeConfusableMatching;
    private readonly bool _asciiOnly;

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
        _unicodeConfusableMatching = options.UnicodeConfusableMatching;
        _asciiOnly = options.AsciiOnly;

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
        if (_asciiOnly && value is not null && !ContainsOnlyPrintableAscii(value))
        {
            return UnclaimableResult.InvalidCharacters(value);
        }

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

        if (_unicodeConfusableMatching)
        {
            ReservedEntry? confusableMatch;
            if (TryMatchUnicodeConfusable(exact, out confusableMatch))
            {
                return CreateReservedResult(value, confusableMatch!, UnclaimableMatchKind.UnicodeConfusable);
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

    private bool TryMatchUnicodeConfusable(string value, out ReservedEntry? match)
    {
        bool changed;
        var skeleton = NormalizeUnicodeConfusables(value, out changed);
        if (!changed)
        {
            match = null;
            return false;
        }

        if (_exact.TryGetValue(skeleton, out match))
        {
            return true;
        }

        if (_compactMatching)
        {
            var compact = NormalizeCompact(skeleton);
            if (compact.Length > 0 && _compact.TryGetValue(compact, out match))
            {
                return true;
            }
        }

        if (_obfuscationMatching && TryMatchObfuscated(skeleton, out match))
        {
            return true;
        }

        match = null;
        return false;
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

    private static string NormalizeUnicodeConfusables(string value, out bool changed)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        changed = false;

        for (var index = 0; index < decomposed.Length; index++)
        {
            var character = decomposed[index];
            var category = CharUnicodeInfo.GetUnicodeCategory(decomposed, index);

            if (category == UnicodeCategory.NonSpacingMark
                || category == UnicodeCategory.SpacingCombiningMark
                || category == UnicodeCategory.EnclosingMark)
            {
                changed = true;
                continue;
            }

            char mapped;
            if (TryMapUnicodeConfusable(character, out mapped))
            {
                builder.Append(mapped);
                changed = true;
            }
            else
            {
                builder.Append(character);
            }

            if (char.IsHighSurrogate(character)
                && index + 1 < decomposed.Length
                && char.IsLowSurrogate(decomposed[index + 1]))
            {
                builder.Append(decomposed[index + 1]);
                index++;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool TryMapUnicodeConfusable(char character, out char mapped)
    {
        switch (character)
        {
            // Cyrillic lookalikes.
            case '\u0430': mapped = 'a'; return true; // а
            case '\u0432': mapped = 'b'; return true; // в
            case '\u0435': mapped = 'e'; return true; // е
            case '\u043A': mapped = 'k'; return true; // к
            case '\u043C': mapped = 'm'; return true; // м
            case '\u043D': mapped = 'h'; return true; // н
            case '\u043E': mapped = 'o'; return true; // о
            case '\u0440': mapped = 'p'; return true; // р
            case '\u0441': mapped = 'c'; return true; // с
            case '\u0442': mapped = 't'; return true; // т
            case '\u0443': mapped = 'y'; return true; // у
            case '\u0445': mapped = 'x'; return true; // х
            case '\u0455': mapped = 's'; return true; // ѕ
            case '\u0456': mapped = 'i'; return true; // і
            case '\u0458': mapped = 'j'; return true; // ј
            case '\u04CF': mapped = 'l'; return true; // ӏ

            // Greek lookalikes.
            case '\u03B1': mapped = 'a'; return true; // α
            case '\u03B2': mapped = 'b'; return true; // β
            case '\u03B5': mapped = 'e'; return true; // ε
            case '\u03B9': mapped = 'i'; return true; // ι
            case '\u03BA': mapped = 'k'; return true; // κ
            case '\u03BC': mapped = 'm'; return true; // μ
            case '\u03BD': mapped = 'v'; return true; // ν
            case '\u03BF': mapped = 'o'; return true; // ο
            case '\u03C1': mapped = 'p'; return true; // ρ
            case '\u03C4': mapped = 't'; return true; // τ
            case '\u03C5': mapped = 'y'; return true; // υ
            case '\u03C7': mapped = 'x'; return true; // χ
            case '\u03F2': mapped = 'c'; return true; // ϲ

            // Latin characters commonly used as visual substitutions.
            case '\u0131': mapped = 'i'; return true; // ı

            default:
                mapped = '\0';
                return false;
        }
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

    private static bool ContainsOnlyPrintableAscii(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character < '\u0020' || character > '\u007E')
            {
                return false;
            }
        }

        return true;
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
