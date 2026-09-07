namespace Unclaimable;

public enum UnclaimableMatchKind
{
    None = 0,
    Exact = 1,
    Compact = 2,
    Obfuscated = 3,
    UnicodeConfusable = 4,
    InvalidCharacters = 5,
    Partial = 6,
    NumbersNotAllowed = 7,
    TooShort = 8,
    TooLong = 9,
    BlockedCharacter = 10,
    LeadingSeparator = 11,
    TrailingSeparator = 12
}

public sealed class UnclaimableResult
{
    public UnclaimableResult(bool isReserved, string? input, string? matchedValue, string? category, UnclaimableMatchKind matchKind)
        : this(isReserved, input, matchedValue, category, matchKind, null, null, null, null)
    {
    }

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

    public bool IsReserved { get; }
    public bool IsClaimable => !IsReserved;
    public string? Input { get; }
    public int InputLength => Input?.Length ?? 0;
    public string? MatchedValue { get; }
    public string? Category { get; }
    public UnclaimableMatchKind MatchKind { get; }
    public int? OffendingCharacterIndex { get; }
    public string? OffendingCharacter { get; }
    public int? MatchStartIndex { get; }
    public int? MatchLength { get; }

    public static UnclaimableResult Allowed(string? input) =>
        new UnclaimableResult(false, input, null, null, UnclaimableMatchKind.None);

    public static UnclaimableResult InvalidCharacters(string? input, int? index = null, string? character = null) =>
        Policy(input, UnclaimableMatchKind.InvalidCharacters, index, character);

    public static UnclaimableResult NumbersNotAllowed(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.NumbersNotAllowed, index, character);

    public static UnclaimableResult TooShort(string? input) =>
        Policy(input, UnclaimableMatchKind.TooShort);

    public static UnclaimableResult TooLong(string? input) =>
        Policy(input, UnclaimableMatchKind.TooLong);

    public static UnclaimableResult BlockedCharacter(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.BlockedCharacter, index, character);

    public static UnclaimableResult LeadingSeparator(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.LeadingSeparator, index, character);

    public static UnclaimableResult TrailingSeparator(string? input, int index, string character) =>
        Policy(input, UnclaimableMatchKind.TrailingSeparator, index, character);

    private static UnclaimableResult Policy(
        string? input,
        UnclaimableMatchKind kind,
        int? index = null,
        string? character = null) =>
        new UnclaimableResult(true, input, null, null, kind, index, character, null, null);
}
