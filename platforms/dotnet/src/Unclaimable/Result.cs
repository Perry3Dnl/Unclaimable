namespace Unclaimable;

/// <summary>Describes the reason a value was accepted or rejected.</summary>
public enum MatchKind
{
    /// <summary>No rejection occurred.</summary>
    None = 0,
    /// <summary>The normalized value exactly matched a reserved entry.</summary>
    Exact = 1,
    /// <summary>The compacted value matched a reserved entry.</summary>
    Compact = 2,
    /// <summary>An obfuscation candidate matched a reserved entry.</summary>
    Obfuscated = 3,
    /// <summary>A Unicode-confusable form matched a reserved entry.</summary>
    UnicodeConfusable = 4,
    /// <summary>The value failed character or Unicode-structure validation.</summary>
    InvalidCharacters = 5,
    /// <summary>A reserved entry matched within a larger value.</summary>
    Partial = 6,
    /// <summary>The value contained a decimal digit while numbers were disabled.</summary>
    NumbersNotAllowed = 7,
    /// <summary>The value was shorter than the configured minimum length.</summary>
    TooShort = 8,
    /// <summary>The value was longer than the configured maximum length.</summary>
    TooLong = 9,
    /// <summary>The value contained a character blocked by the active policy.</summary>
    BlockedCharacter = 10,
    /// <summary>The value started with a separator while leading separators were disabled.</summary>
    LeadingSeparator = 11,
    /// <summary>The value ended with a separator while trailing separators were disabled.</summary>
    TrailingSeparator = 12
}

/// <summary>
/// Represents the first validation or reserved-name result for an input value.
/// Lengths and indexes are measured in UTF-16 code units.
/// </summary>
public sealed class Result
{
    /// <summary>Creates a result without character or match-offset details.</summary>
    public Result(bool isReserved, string? input, string? matchedValue, string? category, MatchKind matchKind)
        : this(isReserved, input, matchedValue, category, matchKind, null, null, null, null)
    {
    }

    /// <summary>
    /// Creates a result with the legacy transformed-text offsets and offending-character details.
    /// <paramref name="matchStartIndex"/> and <paramref name="matchLength"/> keep their existing
    /// meaning and refer to the transformed matching text, not necessarily the original input.
    /// </summary>
    public Result(
        bool isReserved,
        string? input,
        string? matchedValue,
        string? category,
        MatchKind matchKind,
        int? offendingCharacterIndex,
        string? offendingCharacter,
        int? matchStartIndex,
        int? matchLength)
        : this(
            isReserved,
            input,
            matchedValue,
            category,
            matchKind,
            offendingCharacterIndex,
            offendingCharacter,
            matchStartIndex,
            matchLength,
            null,
            null,
            null)
    {
    }

    internal Result(
        bool isReserved,
        string? input,
        string? matchedValue,
        string? category,
        MatchKind matchKind,
        int? offendingCharacterIndex,
        string? offendingCharacter,
        int? matchStartIndex,
        int? matchLength,
        int? originalMatchStartIndex,
        int? originalMatchLength,
        int? lengthLimit)
    {
        IsReserved = isReserved;
        Input = input;
        MatchedValue = matchedValue;
        Category = category;
        MatchKind = matchKind;
        OffendingCharacterIndex = offendingCharacterIndex;
        OffendingCharacter = offendingCharacter;
        MatchStartIndex = matchStartIndex;
        MatchLength = matchLength;
        OriginalMatchStartIndex = originalMatchStartIndex;
        OriginalMatchLength = originalMatchLength;
        LengthLimit = lengthLimit;
    }

    /// <summary>
    /// Gets whether the value is rejected. This includes structural validation failures as well as reserved-name matches.
    /// </summary>
    public bool IsReserved { get; }

    /// <summary>Gets whether the value is claimable.</summary>
    public bool IsClaimable => !IsReserved;

    /// <summary>Gets the original input value.</summary>
    public string? Input { get; }

    /// <summary>Gets the original input length in UTF-16 code units.</summary>
    public int InputLength => Input?.Length ?? 0;

    /// <summary>Gets the reserved dataset value that matched, when applicable.</summary>
    public string? MatchedValue { get; }

    /// <summary>Gets the dataset category of the matched value, when applicable.</summary>
    public string? Category { get; }

    /// <summary>Gets the validation or match reason.</summary>
    public MatchKind MatchKind { get; }

    /// <summary>Gets the UTF-16 index of an offending character in the original input, when applicable.</summary>
    public int? OffendingCharacterIndex { get; }

    /// <summary>Gets the offending Unicode scalar value as a string, when applicable.</summary>
    public string? OffendingCharacter { get; }

    /// <summary>
    /// Gets the start offset in the transformed text used by the matching pipeline.
    /// This legacy offset is measured in UTF-16 code units and is intentionally unchanged.
    /// </summary>
    public int? MatchStartIndex { get; }

    /// <summary>
    /// Gets the matched length in the transformed text used by the matching pipeline.
    /// This legacy length is measured in UTF-16 code units and is intentionally unchanged.
    /// </summary>
    public int? MatchLength { get; }

    /// <summary>Start of the matched span in the original UTF-16 input.</summary>
    public int? OriginalMatchStartIndex { get; }

    /// <summary>Length of the matched span in the original UTF-16 input.</summary>
    public int? OriginalMatchLength { get; }

    /// <summary>The configured length limit responsible for this failure.</summary>
    public int? LengthLimit { get; }

    /// <summary>Creates an allowed result.</summary>
    public static Result Allowed(string? input) =>
        new Result(false, input, null, null, MatchKind.None);

    /// <summary>Creates an invalid-character result.</summary>
    public static Result InvalidCharacters(string? input, int? index = null, string? character = null) =>
        Policy(input, MatchKind.InvalidCharacters, index, character);

    /// <summary>Creates a numbers-not-allowed result.</summary>
    public static Result NumbersNotAllowed(string? input, int index, string character) =>
        Policy(input, MatchKind.NumbersNotAllowed, index, character);

    /// <summary>Creates a too-short result without a captured threshold.</summary>
    public static Result TooShort(string? input) =>
        Policy(input, MatchKind.TooShort);

    /// <summary>Creates a too-long result without a captured threshold.</summary>
    public static Result TooLong(string? input) =>
        Policy(input, MatchKind.TooLong);

    /// <summary>Creates a blocked-character result.</summary>
    public static Result BlockedCharacter(string? input, int index, string character) =>
        Policy(input, MatchKind.BlockedCharacter, index, character);

    /// <summary>Creates a leading-separator result.</summary>
    public static Result LeadingSeparator(string? input, int index, string character) =>
        Policy(input, MatchKind.LeadingSeparator, index, character);

    /// <summary>Creates a trailing-separator result.</summary>
    public static Result TrailingSeparator(string? input, int index, string character) =>
        Policy(input, MatchKind.TrailingSeparator, index, character);

    internal static Result TooShort(string? input, int lengthLimit) =>
        Policy(input, MatchKind.TooShort, lengthLimit: lengthLimit);

    internal static Result TooLong(string? input, int lengthLimit) =>
        Policy(input, MatchKind.TooLong, lengthLimit: lengthLimit);

    private static Result Policy(
        string? input,
        MatchKind kind,
        int? index = null,
        string? character = null,
        int? lengthLimit = null) =>
        new Result(true, input, null, null, kind, index, character, null, null, null, null, lengthLimit);
}
