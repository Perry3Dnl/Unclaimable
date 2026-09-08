using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unclaimable;

public sealed class Checker : IChecker
{
    private const int MaxObfuscationCandidates = 32;

    private sealed class ReservedEntry
    {
        public ReservedEntry(string value, string category, Language? language = null)
        {
            Value = value;
            Category = category;
            Language = language;
        }

        public string Value { get; }
        public string Category { get; }
        public Language? Language { get; }
    }

    private sealed class PartialEntry
    {
        public PartialEntry(string exact, string compact, ReservedEntry entry)
        {
            Exact = exact;
            Compact = compact;
            Entry = entry;
        }

        public string Exact { get; }
        public string Compact { get; }
        public ReservedEntry Entry { get; }
    }

    [DataContract]
    private sealed class ReservedListDocument
    {
        [DataMember(Name = "schema")]
        public int Schema { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; } = string.Empty;

        [DataMember(Name = "language")]
        public string? Language { get; set; }

        [DataMember(Name = "values")]
        public string[] Values { get; set; } = Array.Empty<string>();
    }

    private static readonly Lazy<IReadOnlyList<ReservedEntry>> BuiltInEntries =
        new Lazy<IReadOnlyList<ReservedEntry>>(LoadBuiltInEntries, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Dictionary<string, ReservedEntry> _exact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _compact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly List<PartialEntry> _partialEntries = new List<PartialEntry>();
    private readonly IPolicy _policy;
    private readonly bool _minimumLengthEnabled;
    private readonly bool _maximumLengthEnabled;
    private readonly bool _whitespaceEnabled;
    private readonly bool _blockedCharactersEnabled;
    private readonly bool _leadingSeparatorEnabled;
    private readonly bool _trailingSeparatorEnabled;
    private readonly int _minimumLength;
    private readonly int _maximumLength;
    private readonly bool _compactMatching;
    private readonly bool _partialMatching;
    private readonly int _partialMatchMinimumLength;
    private readonly bool _obfuscationMatching;
    private readonly bool _unicodeConfusableMatching;
    private readonly bool _allowNumbers;
    private readonly bool _asciiOnly;

    public static Checker Default { get; } = new Checker();

    public Checker()
        : this(new Options())
    {
    }

    public Checker(Options options)
        : this(options, new Policy(options?.ConfiguredBlockedCharacters ?? Array.Empty<string>()))
    {
    }

    public Checker(Options options, IPolicy policy)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        foreach (var language in options.Languages)
        {
            if (!Enum.IsDefined(typeof(Language), language))
            {
                throw new ArgumentOutOfRangeException(nameof(options.Languages), "Languages must contain only supported Language values.");
            }
        }

        if (options.MinimumLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MinimumLength), "MinimumLength cannot be negative.");
        }

        if (options.MaximumLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaximumLength), "MaximumLength must be at least 1.");
        }

        if (options.MaximumLength < options.MinimumLength)
        {
            throw new ArgumentException("MaximumLength must be greater than or equal to MinimumLength.", nameof(options));
        }

        if (options.PartialMatchMinimumLength < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.PartialMatchMinimumLength),
                "PartialMatchMinimumLength must be at least 1.");
        }

        _policy = policy;
        _minimumLengthEnabled = options.IsRuleEnabled(Rule.MinimumLength);
        _maximumLengthEnabled = options.IsRuleEnabled(Rule.MaximumLength);
        _whitespaceEnabled = options.IsRuleEnabled(Rule.Whitespace);
        _blockedCharactersEnabled = options.IsRuleEnabled(Rule.BlockedCharacters);
        _leadingSeparatorEnabled = options.IsRuleEnabled(Rule.LeadingSeparator);
        _trailingSeparatorEnabled = options.IsRuleEnabled(Rule.TrailingSeparator);
        _minimumLength = options.MinimumLength;
        _maximumLength = options.MaximumLength;
        _compactMatching = options.CompactMatching && options.IsRuleEnabled(Rule.CompactMatching);
        _partialMatching = options.PartialMatching && options.IsRuleEnabled(Rule.PartialMatching);
        _partialMatchMinimumLength = options.PartialMatchMinimumLength;
        _obfuscationMatching = options.ObfuscationMatching && options.IsRuleEnabled(Rule.ObfuscationMatching);
        _unicodeConfusableMatching = options.UnicodeConfusableMatching && options.IsRuleEnabled(Rule.UnicodeConfusableMatching);
        _allowNumbers = options.AllowNumbers || !options.IsRuleEnabled(Rule.Numbers);
        _asciiOnly = options.AsciiOnly;

        var profanityMatching = options.ProfanityMatching && options.IsRuleEnabled(Rule.Profanity);

        foreach (var entry in BuiltInEntries.Value)
        {
            if (entry.Language.HasValue && !options.Languages.Contains(entry.Language.Value))
            {
                continue;
            }

            var isProfanity = string.Equals(entry.Category, "profanity", StringComparison.Ordinal);
            if (isProfanity && !profanityMatching)
            {
                continue;
            }

            Add(entry, includeInPartialMatching: !isProfanity || options.ProfanityPartialMatching);
        }

        foreach (var value in options.AdditionalReserved)
        {
            Add(new ReservedEntry(value, "custom"));
        }

        _partialEntries.Sort((left, right) => right.Compact.Length.CompareTo(left.Compact.Length));
    }

    public bool IsReserved(string? value) => Check(value).IsReserved;

    public bool IsClaimable(string? value) => !Check(value).IsReserved;

    public Result Check(string? value)
    {
        Result? policyViolation;
        if (TryFindFirstPolicyViolation(value, out policyViolation))
        {
            return policyViolation!;
        }

        return CheckReservedName(value);
    }

    public DetailedResult CheckDetailed(string? value, bool includeMessages = false)
    {
        var diagnostics = new List<Diagnostic>();
        CollectPolicyDiagnostics(value, includeMessages, diagnostics);

        var reservedResult = CheckReservedName(value);
        if (reservedResult.IsReserved)
        {
            diagnostics.Add(ToDiagnostic(reservedResult, includeMessages));
        }

        return new DetailedResult(value, diagnostics);
    }

    private Result CheckReservedName(string? value)
    {
        var exact = NormalizeExact(value);
        if (exact is null)
        {
            return Result.Allowed(value);
        }

        ReservedEntry? exactMatch;
        if (_exact.TryGetValue(exact, out exactMatch))
        {
            return CreateReservedResult(value, exactMatch, MatchKind.Exact, 0, exact.Length);
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching)
        {
            ReservedEntry? compactMatch;
            if (compact.Length > 0 && _compact.TryGetValue(compact, out compactMatch))
            {
                return CreateReservedResult(value, compactMatch, MatchKind.Compact, 0, compact.Length);
            }
        }

        if (_partialMatching)
        {
            ReservedEntry? partialMatch;
            int partialStart;
            int partialLength;
            if (TryMatchPartial(exact, compact, out partialMatch, out partialStart, out partialLength))
            {
                return CreateReservedResult(value, partialMatch!, MatchKind.Partial, partialStart, partialLength);
            }
        }

        if (_unicodeConfusableMatching)
        {
            ReservedEntry? confusableMatch;
            MatchKind confusableKind;
            int? confusableStart;
            int? confusableLength;
            if (TryMatchUnicodeConfusable(
                    exact,
                    out confusableMatch,
                    out confusableKind,
                    out confusableStart,
                    out confusableLength))
            {
                return CreateReservedResult(value, confusableMatch!, confusableKind, confusableStart, confusableLength);
            }
        }

        if (_obfuscationMatching)
        {
            ReservedEntry? obfuscatedMatch;
            MatchKind obfuscatedKind;
            int? obfuscatedStart;
            int? obfuscatedLength;
            if (TryMatchObfuscated(
                    exact,
                    out obfuscatedMatch,
                    out obfuscatedKind,
                    out obfuscatedStart,
                    out obfuscatedLength))
            {
                return CreateReservedResult(value, obfuscatedMatch!, obfuscatedKind, obfuscatedStart, obfuscatedLength);
            }
        }

        return Result.Allowed(value);
    }

    private static Result CreateReservedResult(
        string? input,
        ReservedEntry match,
        MatchKind matchKind,
        int? matchStartIndex = null,
        int? matchLength = null)
    {
        return new Result(
            true,
            input,
            match.Value,
            match.Category,
            matchKind,
            null,
            null,
            matchStartIndex,
            matchLength);
    }

    private void Add(ReservedEntry entry, bool includeInPartialMatching = true)
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

        if (includeInPartialMatching && compact.Length >= _partialMatchMinimumLength)
        {
            _partialEntries.Add(new PartialEntry(exact, compact, entry));
        }
    }

    private bool TryMatchPartial(
        string exact,
        string compact,
        out ReservedEntry? match,
        out int startIndex,
        out int matchLength)
    {
        foreach (var partial in _partialEntries)
        {
            var exactIndex = exact.IndexOf(partial.Exact, StringComparison.Ordinal);
            if (exactIndex >= 0 && exact.Length > partial.Exact.Length)
            {
                match = partial.Entry;
                startIndex = exactIndex;
                matchLength = partial.Exact.Length;
                return true;
            }

            if (compact.Length > partial.Compact.Length)
            {
                var compactIndex = compact.IndexOf(partial.Compact, StringComparison.Ordinal);
                if (compactIndex >= 0)
                {
                    match = partial.Entry;
                    startIndex = compactIndex;
                    matchLength = partial.Compact.Length;
                    return true;
                }
            }
        }

        match = null;
        startIndex = -1;
        matchLength = 0;
        return false;
    }

    private bool TryMatchUnicodeConfusable(
        string value,
        out ReservedEntry? match,
        out MatchKind matchKind,
        out int? matchStartIndex,
        out int? matchLength)
    {
        bool changed;
        var skeleton = NormalizeUnicodeConfusables(value, out changed);
        if (!changed)
        {
            match = null;
            matchKind = MatchKind.None;
            matchStartIndex = null;
            matchLength = null;
            return false;
        }

        if (_exact.TryGetValue(skeleton, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = skeleton.Length;
            return true;
        }

        var compact = NormalizeCompact(skeleton);
        if (_compactMatching && compact.Length > 0 && _compact.TryGetValue(compact, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = compact.Length;
            return true;
        }

        if (_partialMatching)
        {
            int partialStart;
            int partialLength;
            if (TryMatchPartial(skeleton, compact, out match, out partialStart, out partialLength))
            {
                matchKind = MatchKind.Partial;
                matchStartIndex = partialStart;
                matchLength = partialLength;
                return true;
            }
        }

        if (_obfuscationMatching
            && TryMatchObfuscated(skeleton, out match, out matchKind, out matchStartIndex, out matchLength))
        {
            return true;
        }

        match = null;
        matchKind = MatchKind.None;
        matchStartIndex = null;
        matchLength = null;
        return false;
    }

    private bool TryMatchObfuscated(
        string value,
        out ReservedEntry? match,
        out MatchKind matchKind,
        out int? matchStartIndex,
        out int? matchLength)
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
            matchKind = MatchKind.None;
            matchStartIndex = null;
            matchLength = null;
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Length > 0 && _compact.TryGetValue(candidate, out match))
            {
                matchKind = MatchKind.Obfuscated;
                matchStartIndex = 0;
                matchLength = candidate.Length;
                return true;
            }

            if (_partialMatching)
            {
                foreach (var partial in _partialEntries)
                {
                    if (candidate.Length <= partial.Compact.Length)
                    {
                        continue;
                    }

                    var partialIndex = candidate.IndexOf(partial.Compact, StringComparison.Ordinal);
                    if (partialIndex >= 0)
                    {
                        match = partial.Entry;
                        matchKind = MatchKind.Partial;
                        matchStartIndex = partialIndex;
                        matchLength = partial.Compact.Length;
                        return true;
                    }
                }
            }
        }

        match = null;
        matchKind = MatchKind.None;
        matchStartIndex = null;
        matchLength = null;
        return false;
    }

    private bool TryFindFirstPolicyViolation(string? value, out Result? violation)
    {
        violation = null;
        if (value is null)
        {
            return false;
        }

        if (_minimumLengthEnabled && value.Length < _minimumLength)
        {
            violation = Result.TooShort(value);
            return true;
        }

        if (_maximumLengthEnabled && value.Length > _maximumLength)
        {
            violation = Result.TooLong(value);
            return true;
        }

        if (value.Length == 0)
        {
            return false;
        }

        if (_leadingSeparatorEnabled && IsSeparator(value[0]))
        {
            violation = Result.LeadingSeparator(value, 0, value[0].ToString());
            return true;
        }

        if (_trailingSeparatorEnabled && IsSeparator(value[value.Length - 1]))
        {
            violation = Result.TrailingSeparator(value, value.Length - 1, value[value.Length - 1].ToString());
            return true;
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var characterText = character.ToString();
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                characterText = new string(new[] { character, value[index + 1] });
            }

            if (!_allowNumbers && category == UnicodeCategory.DecimalDigitNumber)
            {
                violation = Result.NumbersNotAllowed(value, index, characterText);
                return true;
            }

            if (_asciiOnly && (character < '\u0020' || character > '\u007E'))
            {
                violation = Result.InvalidCharacters(value, index, characterText);
                return true;
            }

            var explicitlyAllowed = _policy.IsCharacterExplicitlyAllowed(characterText);
            if (!explicitlyAllowed && _whitespaceEnabled && IsWhitespace(category, character))
            {
                violation = Result.BlockedCharacter(value, index, characterText);
                return true;
            }

            if (!explicitlyAllowed && _blockedCharactersEnabled && _policy.IsCharacterBlocked(characterText))
            {
                violation = Result.BlockedCharacter(value, index, characterText);
                return true;
            }

            if (characterText.Length == 2)
            {
                index++;
            }
        }

        return false;
    }

    private void CollectPolicyDiagnostics(
        string? value,
        bool includeMessages,
        List<Diagnostic> diagnostics)
    {
        if (value is null)
        {
            return;
        }

        if (_minimumLengthEnabled && value.Length < _minimumLength)
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.TooShort,
                message: includeMessages ? $"Value must be at least {_minimumLength} characters long." : null));
        }

        if (_maximumLengthEnabled && value.Length > _maximumLength)
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.TooLong,
                message: includeMessages ? $"Value must be no more than {_maximumLength} characters long." : null));
        }

        if (value.Length == 0)
        {
            return;
        }

        if (_leadingSeparatorEnabled && IsSeparator(value[0]))
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.LeadingSeparator,
                offendingCharacterIndex: 0,
                offendingCharacter: value[0].ToString(),
                message: includeMessages ? "Leading separators are not allowed." : null));
        }

        if (_trailingSeparatorEnabled && IsSeparator(value[value.Length - 1]))
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.TrailingSeparator,
                offendingCharacterIndex: value.Length - 1,
                offendingCharacter: value[value.Length - 1].ToString(),
                message: includeMessages ? "Trailing separators are not allowed." : null));
        }

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            var characterText = character.ToString();
            var category = CharUnicodeInfo.GetUnicodeCategory(value, index);

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                characterText = new string(new[] { character, value[index + 1] });
            }

            if (!_allowNumbers && category == UnicodeCategory.DecimalDigitNumber)
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.NumbersNotAllowed,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages
                        ? $"Numbers are not allowed; '{characterText}' at index {index} is not permitted."
                        : null));
            }

            if (_asciiOnly && (character < '\u0020' || character > '\u007E'))
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.InvalidCharacters,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages
                        ? $"Character '{characterText}' at index {index} is not allowed by the ASCII-only policy."
                        : null));
            }

            var explicitlyAllowed = _policy.IsCharacterExplicitlyAllowed(characterText);
            if (!explicitlyAllowed && _whitespaceEnabled && IsWhitespace(category, character))
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.BlockedCharacter,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages ? $"Character '{characterText}' at index {index} is blocked." : null));
            }
            else if (!explicitlyAllowed && _blockedCharactersEnabled && _policy.IsCharacterBlocked(characterText))
            {
                diagnostics.Add(new Diagnostic(
                    MatchKind.BlockedCharacter,
                    offendingCharacterIndex: index,
                    offendingCharacter: characterText,
                    message: includeMessages ? $"Character '{characterText}' at index {index} is blocked." : null));
            }

            if (characterText.Length == 2)
            {
                index++;
            }
        }
    }

    private static Diagnostic ToDiagnostic(Result result, bool includeMessage)
    {
        return new Diagnostic(
            result.MatchKind,
            result.MatchedValue,
            result.Category,
            result.OffendingCharacterIndex,
            result.OffendingCharacter,
            result.MatchStartIndex,
            result.MatchLength,
            includeMessage ? BuildMessage(result) : null);
    }

    private static string BuildMessage(Result result)
    {
        switch (result.MatchKind)
        {
            case MatchKind.Exact:
                return $"'{result.MatchedValue}' is reserved and cannot be claimed.";
            case MatchKind.Compact:
                return $"This value resolves to the reserved value '{result.MatchedValue}' after separators or punctuation are ignored.";
            case MatchKind.Partial:
                return $"This value contains the reserved value '{result.MatchedValue}'.";
            case MatchKind.Obfuscated:
                return $"This value appears to obfuscate the reserved value '{result.MatchedValue}'.";
            case MatchKind.UnicodeConfusable:
                return $"This value contains Unicode lookalikes that resolve to the reserved value '{result.MatchedValue}'.";
            case MatchKind.NumbersNotAllowed:
                return $"Numbers are not allowed; '{result.OffendingCharacter}' at index {result.OffendingCharacterIndex} is not permitted.";
            case MatchKind.InvalidCharacters:
                return $"Character '{result.OffendingCharacter}' at index {result.OffendingCharacterIndex} is not allowed.";
            case MatchKind.TooShort:
                return "This value is shorter than the configured minimum length.";
            case MatchKind.TooLong:
                return "This value is longer than the configured maximum length.";
            case MatchKind.BlockedCharacter:
                return $"Character '{result.OffendingCharacter}' at index {result.OffendingCharacterIndex} is blocked.";
            case MatchKind.LeadingSeparator:
                return "Leading separators are not allowed.";
            case MatchKind.TrailingSeparator:
                return "Trailing separators are not allowed.";
            default:
                return "This value is not allowed.";
        }
    }

    private static bool IsSeparator(char character) =>
        character == '-' || character == '_' || character == '.';

    private static bool IsWhitespace(UnicodeCategory category, char character) =>
        char.IsWhiteSpace(character)
        || category == UnicodeCategory.SpaceSeparator
        || category == UnicodeCategory.LineSeparator
        || category == UnicodeCategory.ParagraphSeparator;

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
            case '\u0430': mapped = 'a'; return true;
            case '\u0432': mapped = 'b'; return true;
            case '\u0435': mapped = 'e'; return true;
            case '\u043A': mapped = 'k'; return true;
            case '\u043C': mapped = 'm'; return true;
            case '\u043D': mapped = 'h'; return true;
            case '\u043E': mapped = 'o'; return true;
            case '\u0440': mapped = 'p'; return true;
            case '\u0441': mapped = 'c'; return true;
            case '\u0442': mapped = 't'; return true;
            case '\u0443': mapped = 'y'; return true;
            case '\u0445': mapped = 'x'; return true;
            case '\u0455': mapped = 's'; return true;
            case '\u0456': mapped = 'i'; return true;
            case '\u0458': mapped = 'j'; return true;
            case '\u04CF': mapped = 'l'; return true;
            case '\u03B1': mapped = 'a'; return true;
            case '\u03B2': mapped = 'b'; return true;
            case '\u03B5': mapped = 'e'; return true;
            case '\u03B9': mapped = 'i'; return true;
            case '\u03BA': mapped = 'k'; return true;
            case '\u03BC': mapped = 'm'; return true;
            case '\u03BD': mapped = 'v'; return true;
            case '\u03BF': mapped = 'o'; return true;
            case '\u03C1': mapped = 'p'; return true;
            case '\u03C4': mapped = 't'; return true;
            case '\u03C5': mapped = 'y'; return true;
            case '\u03C7': mapped = 'x'; return true;
            case '\u03F2': mapped = 'c'; return true;
            case '\u0131': mapped = 'i'; return true;
            default:
                mapped = '\0';
                return false;
        }
    }

    private static List<string> ExpandCandidates(List<string> candidates, string[] substitutions)
    {
        var expanded = new List<string>(Math.Min(MaxObfuscationCandidates, candidates.Count * substitutions.Length));

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
            case '0': substitutions = new[] { "o" }; return true;
            case '1': substitutions = new[] { "i", "l" }; return true;
            case '2': substitutions = new[] { "z" }; return true;
            case '3': substitutions = new[] { "e" }; return true;
            case '4': substitutions = new[] { "a" }; return true;
            case '5': substitutions = new[] { "s" }; return true;
            case '6':
            case '9': substitutions = new[] { "g" }; return true;
            case '7': substitutions = new[] { "t" }; return true;
            case '8': substitutions = new[] { "b" }; return true;
            case '@': substitutions = new[] { "a" }; return true;
            case '$': substitutions = new[] { "s" }; return true;
            case '!':
            case '|': substitutions = new[] { "i", "l" }; return true;
            case '+': substitutions = new[] { "t" }; return true;
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

        return value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
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
        var assembly = typeof(Checker).Assembly;
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

                var language = ResolveDatasetLanguage(document, resourceName);
                entries.AddRange(document.Values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => new ReservedEntry(value, document.Category, language)));
            }
        }

        return entries;
    }

    private static Language? ResolveDatasetLanguage(ReservedListDocument document, string resourceName)
    {
        var language = document.Language?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(language))
        {
            if (string.Equals(document.Category, "brands", StringComparison.Ordinal)
                || string.Equals(document.Category, "technology", StringComparison.Ordinal))
            {
                return null;
            }

            return Language.English;
        }

        switch (language)
        {
            case "global":
                return null;
            case "en":
            case "eng":
            case "english":
                return Language.English;
            case "nl":
            case "nld":
            case "dutch":
                return Language.Dutch;
            case "de":
            case "deu":
            case "ger":
            case "german":
            case "deutsch":
                return Language.German;
            case "fr":
            case "fra":
            case "fre":
            case "french":
            case "francais":
                return Language.French;
            case "es":
            case "spa":
            case "spanish":
            case "espanol":
                return Language.Spanish;
            case "it":
            case "ita":
            case "italian":
            case "italiano":
                return Language.Italian;
            case "pt":
            case "por":
            case "portuguese":
            case "portugues":
                return Language.Portuguese;
            case "pl":
            case "pol":
            case "polish":
                return Language.Polish;
            case "tr":
            case "tur":
            case "turkish":
                return Language.Turkish;
            case "id":
            case "ind":
            case "indonesian":
                return Language.Indonesian;
            case "cs":
            case "ces":
            case "cze":
            case "czech":
                return Language.Czech;
            case "vi":
            case "vie":
            case "vietnamese":
                return Language.Vietnamese;
            case "hu":
            case "hun":
            case "hungarian":
                return Language.Hungarian;
            case "sv":
            case "swe":
            case "swedish":
                return Language.Swedish;
            case "ro":
            case "ron":
            case "rum":
            case "romanian":
                return Language.Romanian;
            default:
                throw new InvalidOperationException(
                    $"Embedded dataset '{resourceName}' declares unsupported language '{language}'.");
        }
    }
}
