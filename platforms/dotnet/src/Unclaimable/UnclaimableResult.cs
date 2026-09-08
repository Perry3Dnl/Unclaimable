namespace Unclaimable;

/// <summary>
/// Identifies the rule or matching strategy responsible for an Unclaimable result.
/// </summary>
public enum UnclaimableMatchKind
{
    /// <summary>No rejection was detected.</summary>
    None = 0,

    /// <summary>The normalized identifier exactly matched a reserved value.</summary>
    Exact = 1,

    /// <summary>The identifier matched a reserved value after separators or punctuation were removed.</summary>
    Compact = 2,

    /// <summary>The identifier matched a reserved value after common obfuscation substitutions were resolved.</summary>
    Obfuscated = 3,

    /// <summary>The identifier matched a reserved value after Unicode lookalikes or diacritics were normalized.</summary>
    UnicodeConfusable = 4,

    /// <summary>The identifier contains a character rejected by an additional character policy such as ASCII-only mode.</summary>
    InvalidCharacters = 5,

    /// <summary>The identifier contains a reserved value as part of a larger value.</summary>
    Partial = 6,

    /// <summary>The identifier contains a Unicode decimal digit while numbers are disabled.</summary>
    NumbersNotAllowed = 7,

    /// <summary>The identifier is shorter than the configured minimum length.</summary>
    TooShort = 8,

    /// <summary>The identifier is longer than the configured maximum length.</summary>
    TooLong = 9,

    /// <summary>The identifier contains a character blocked by the configured character policy.</summary>
    BlockedCharacter = 10,

    /// <summary>The identifier starts with a separator while leading separators are disabled.</summary>
    LeadingSeparator = 11,

    /// <summary>The identifier ends with a separator while trailing separators are disabled.</summary>
    TrailingSeparator = 12
}

/// <summary>
/// Represents the first validation outcome detected for an identifier.
/// </summary>
public sealed class UnclaimableResult
{
    /// <summary>
    /// Initializes a validation result without character or match-position metadata.
    /// </summary>
    /// <param name="isReserved">Whether the identifier was rejected.</param>
    /// <param name="input">The original identifier that was evaluated.</param>
    /// <param name="matchedValue">The reserved dataset value that matched, when applicable.</param>
    /// <param name="category">The dataset category of the matched value, when applicable.</param>
    /// <param name="matchKind">The rule or matching strategy responsible for the result.</param>
    public UnclaimableResult(bool isReserved, string? input, string? matchedValue, string? category, UnclaimableMatchKind matchKind)
        : this(isReserved, input, matchedValue, category, matchKind, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a validation result with optional character and match-position metadata.
    /// </summary>
    /// <param name="isReserved">Whether the identifier was rejected.</param>
    /// <param name="input">The original identifier that was evaluated.</param>
    /// <param name="matchedValue">The reserved dataset value that matched, when applicable.</param>
    /// <param name="category">The dataset category of the matched value, when applicable.</param>
    /// <param name="matchKind">The rule or matching strategy responsible for the result.</param>
    /// <param name="offendingCharacterIndex">The zero-based UTF-16 index of an offending character, when applicable.</param>
    /// <param name="offendingCharacter">The offending Unicode character, when applicable.</param>
    /// <param name="matchStartIndex">The zero-based start index of a reserved-name match, when available.</param>
    /// <param name="matchLength">The length of the reserved-name match, when available.</param>
    public UnclaimableResult(
        bool isReserved,
        string? input,
        string? matchedValue,
        string? category,
        UnclaimableMatchKind matchKind,
        int? offendingCharacterIndex,
        string? offendingCharacter,
        int? matchStartIndex,
        int? matchLength)
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
    }

    /// <summary>Gets whether the identifier was rejected by a policy or reserved-name rule.</summary>
    public bool IsReserved { get; }

    /// <summary>Gets whether the identifier is allowed under the configured policy.</summary>
    public bool IsClaimable => !IsReserved;

    /// <summary>Gets the original identifier that was evaluated.</summary>
    public string? Input { get; }

    /// <summary>Gets the UTF-16 length of <see cref="Input"/>, or zero when the input is <see langword="null"/>.</summary>
    public int InputLength => Input?.Length ?? 0;

    /// <summary>Gets the reserved dataset value that matched, when applicable.</summary>
    public string? MatchedValue { get; }

    /// <summary>Gets the dataset category of the matched value, when applicable.</summary>
    public string? Category { get; }

    /// <summary>Gets the rule or matching strategy responsible for the result.</summary>
    public UnclaimableMatchKind MatchKind { get; }

    /// <summary>Gets the zero-based UTF-16 index of the offending character, when applicable.</summary>
    public int? OffendingCharacterIndex { get; }

    /// <summary>Gets the offending Unicode character, when applicable.</summary>
    public string? OffendingCharacter { get; }

    /// <summary>Gets the zero-based start index of the reserved-name match, when available.</summary>
    public int? MatchStartIndex { get; }

    /// <summary>Gets the length of the reserved-name match, when available.</summary>
    public int? MatchLength { get; }

    /// <summary>
    /// Creates an allowed result for the supplied input.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <returns>An allowed validation result.</returns>
    public static UnclaimableResult Allowed(string? input) =>
        new UnclaimableResult(false, input, null, null, UnclaimableMatchKind.None);

    /// <summary>
    /// Creates a result for an identifier containing a character rejected by an additional character policy.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <param name="index">The zero-based UTF-16 index of the offending character.</param>
    /// <param name="character">The offending Unicode character.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult InvalidCharacters(string? input, int? index = null, string? character = null) =>
        Policy(input, UnclaimableMatchKind.InvalidCharacters, index, character);

    /// <summary>
    /// Creates a result for an identifier containing a number while numbers are disabled.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <param name="index">The zero-based UTF-16 index of the offending character.</param>
    /// <param name="character">The offending numeric character.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult NumbersNotAllowed(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.NumbersNotAllowed, index, character);

    /// <summary>
    /// Creates a result for an identifier shorter than the configured minimum length.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult TooShort(string? input) =>
        Policy(input, UnclaimableMatchKind.TooShort);

    /// <summary>
    /// Creates a result for an identifier longer than the configured maximum length.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult TooLong(string? input) =>
        Policy(input, UnclaimableMatchKind.TooLong);

    /// <summary>
    /// Creates a result for an identifier containing a blocked character.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <param name="index">The zero-based UTF-16 index of the blocked character.</param>
    /// <param name="character">The blocked Unicode character.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult BlockedCharacter(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.BlockedCharacter, index, character);

    /// <summary>
    /// Creates a result for an identifier that begins with a separator.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <param name="index">The zero-based UTF-16 index of the leading separator.</param>
    /// <param name="character">The leading separator.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult LeadingSeparator(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.LeadingSeparator, index, character);

    /// <summary>
    /// Creates a result for an identifier that ends with a separator.
    /// </summary>
    /// <param name="input">The identifier that was evaluated.</param>
    /// <param name="index">The zero-based UTF-16 index of the trailing separator.</param>
    /// <param name="character">The trailing separator.</param>
    /// <returns>A rejected validation result.</returns>
    public static UnclaimableResult TrailingSeparator(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.TrailingSeparator, index, character);

    private static UnclaimableResult Policy(
        string? input,
        UnclaimableMatchKind kind,
        int? index = null,
        string? character = null) =>
        new UnclaimableResult(true, input, null, null, kind, index, character, null, null);
}
