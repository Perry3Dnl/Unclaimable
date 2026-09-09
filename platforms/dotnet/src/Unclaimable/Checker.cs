using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unclaimable;

/// <summary>
/// Validates identifier structure and checks normalized values against Unclaimable's reserved-name datasets.
/// Configuration from <see cref="Options"/> is captured at construction time, while updates to the supplied
/// <see cref="IPolicy"/> remain live. Null values are accepted; required-field validation remains separate.
/// </summary>
public sealed partial class Checker : IChecker
{
    private const int MaxObfuscationCandidates = 32;

    private sealed class ReservedEntry
    {
        public ReservedEntry(string value, string category, Language? language = null, bool safePartial = false)
        {
            Value = value;
            Category = category;
            Language = language;
            SafePartial = safePartial;
        }

        public string Value { get; }
        public string Category { get; }
        public Language? Language { get; }
        public bool SafePartial { get; }
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

    private sealed class InputMapping
    {
        public InputMapping(int[] exactToOriginal, int[] compactToOriginal)
        {
            ExactToOriginal = exactToOriginal;
            CompactToOriginal = compactToOriginal;
        }

        public int[] ExactToOriginal { get; }
        public int[] CompactToOriginal { get; }
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
        public string[]? Values { get; set; }

        [DataMember(Name = "partialValues")]
        public string[]? PartialValues { get; set; }

        [DataMember(Name = "combinations")]
        public CombinationDocument[]? Combinations { get; set; }
    }

    [DataContract]
    private sealed class CombinationDocument
    {
        [DataMember(Name = "roots")]
        public string[]? Roots { get; set; }

        [DataMember(Name = "suffixes")]
        public string[]? Suffixes { get; set; }

        [DataMember(Name = "partial")]
        public bool Partial { get; set; }
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
    private readonly bool _consistentCompactMatching;
    private readonly bool _partialMatching;
    private readonly int _partialMatchMinimumLength;
    private readonly bool _obfuscationMatching;
    private readonly bool _unicodeConfusableMatching;
    private readonly bool _allowNumbers;
    private readonly bool _asciiOnly;
    private readonly bool _rejectInvisibleOnlyIdentifiers;
    private readonly bool _rejectControlCharacters;
    private readonly bool _rejectFormatCharacters;

    /// <summary>Gets the shared checker using default options and its own live runtime policy.</summary>
    public static Checker Default { get; } = new Checker();

    /// <summary>Creates a checker using default options.</summary>
    public Checker()
        : this(new Options())
    {
    }

    /// <summary>
    /// Creates a checker from options. All option values are captured during construction.
    /// The created runtime character policy remains mutable and live.
    /// </summary>
    public Checker(Options options)
        : this(options, new Policy(options?.ConfiguredBlockedCharacters ?? Array.Empty<string>()))
    {
    }

    /// <summary>
    /// Creates a checker from captured options and a runtime-adjustable character policy.
    /// Option mutations after construction do not affect this checker; policy mutations do.
    /// </summary>
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
        _consistentCompactMatching = options.ConsistentCompactMatching;
        _partialMatching = options.PartialMatching && options.IsRuleEnabled(Rule.PartialMatching);
        _partialMatchMinimumLength = options.PartialMatchMinimumLength;
        _obfuscationMatching = options.ObfuscationMatching && options.IsRuleEnabled(Rule.ObfuscationMatching);
        _unicodeConfusableMatching = options.UnicodeConfusableMatching && options.IsRuleEnabled(Rule.UnicodeConfusableMatching);
        _allowNumbers = options.AllowNumbers || !options.IsRuleEnabled(Rule.Numbers);
        _asciiOnly = options.AsciiOnly;
        _rejectInvisibleOnlyIdentifiers = options.RejectInvisibleOnlyIdentifiers;
        _rejectControlCharacters = options.RejectControlCharacters;
        _rejectFormatCharacters = options.RejectFormatCharacters;

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

            Add(
                entry,
                includeInPartialMatching: entry.SafePartial || !isProfanity || options.ProfanityPartialMatching);
        }

        foreach (var value in options.AdditionalReserved)
        {
            Add(new ReservedEntry(value, "custom"));
        }

        _partialEntries.Sort((left, right) => right.Compact.Length.CompareTo(left.Compact.Length));
    }

    /// <summary>
    /// Returns whether the value is rejected. Structural validation failures are included.
    /// Null is accepted; use required-field validation separately when null is not allowed.
    /// </summary>
    public bool IsReserved(string? value) => Check(value).IsReserved;

    /// <summary>
    /// Returns whether the value is claimable. Null is accepted; required-field validation remains separate.
    /// </summary>
    public bool IsClaimable(string? value) => !Check(value).IsReserved;

    /// <summary>Returns the first validation or reserved-name result.</summary>
    public Result Check(string? value)
    {
        Result? policyViolation;
        if (TryFindFirstPolicyViolation(value, out policyViolation))
        {
            return policyViolation!;
        }

        return CheckReservedName(value);
    }

    /// <summary>
    /// Returns all validation diagnostics plus the reserved-name result, if any.
    /// Existing match offsets refer to transformed matching text; original-input spans are supplied separately when reliable.
    /// </summary>
    public DetailedResult CheckDetailed(string? value, bool includeMessages = false)
    {
        var diagnostics = new List<Diagnostic>();

        if (value is not null && TryFindMalformedUtf16(value, out var invalidIndex, out var invalidCharacter))
        {
            diagnostics.Add(new Diagnostic(
                MatchKind.InvalidCharacters,
                offendingCharacterIndex: invalidIndex,
                offendingCharacter: invalidCharacter,
                message: includeMessages
                    ? $"Character '{invalidCharacter}' at index {invalidIndex} is not a valid UTF-16 sequence."
                    : null));

            return new DetailedResult(value, diagnostics);
        }

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

        var mapping = value is null ? null : TryCreateInputMapping(value, exact);

        ReservedEntry? exactMatch;
        if (_exact.TryGetValue(exact, out exactMatch))
        {
            TryMapOriginalSpan(mapping?.ExactToOriginal, 0, exact.Length, out var originalStart, out var originalLength);
            return CreateReservedResult(
                value,
                exactMatch,
                MatchKind.Exact,
                0,
                exact.Length,
                originalStart,
                originalLength);
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching)
        {
            ReservedEntry? compactMatch;
            if (compact.Length > 0 && _compact.TryGetValue(compact, out compactMatch))
            {
                TryMapOriginalSpan(mapping?.CompactToOriginal, 0, compact.Length, out var originalStart, out var originalLength);
                return CreateReservedResult(
                    value,
                    compactMatch,
                    MatchKind.Compact,
                    0,
                    compact.Length,
                    originalStart,
                    originalLength);
            }
        }

        if (_partialMatching)
        {
            ReservedEntry? partialMatch;
            int partialStart;
            int partialLength;
            bool usedCompact;
            if (TryMatchPartial(exact, compact, out partialMatch, out partialStart, out partialLength, out usedCompact))
            {
                var sourceMap = usedCompact ? mapping?.CompactToOriginal : mapping?.ExactToOriginal;
                TryMapOriginalSpan(sourceMap, partialStart, partialLength, out var originalStart, out var originalLength);
                return CreateReservedResult(
                    value,
                    partialMatch!,
                    MatchKind.Partial,
                    partialStart,
                    partialLength,
                    originalStart,
                    originalLength);
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
        int? matchLength = null,
        int? originalMatchStartIndex = null,
        int? originalMatchLength = null)
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
            matchLength,
            originalMatchStartIndex,
            originalMatchLength,
            null);
    }

}
