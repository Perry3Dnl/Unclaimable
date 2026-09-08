namespace Unclaimable;

public enum MatchKind
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

public sealed class Result
{
    public Result(bool isReserved, string? input, string? matchedValue, string? category, MatchKind matchKind)
        : this(isReserved, input, matchedValue, category, matchKind, null, null, null, null)
    {
    }

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
    public MatchKind MatchKind { get; }
    public int? OffendingCharacterIndex { get; }
    public string? OffendingCharacter { get; }
    public int? MatchStartIndex { get; }
    public int? MatchLength { get; }

    public static Result Allowed(string? input) =>
        new Result(false, input, null, null, MatchKind.None);

    public static Result InvalidCharacters(string? input, int? index = null, string? character = null) =>
        Policy(input, MatchKind.InvalidCharacters, index, character);

    public static Result NumbersNotAllowed(string? input, int index, string character) =>
        Policy(input, MatchKind.NumbersNotAllowed, index, character);

    public static Result TooShort(string? input) =>
        Policy(input, MatchKind.TooShort);

    public static Result TooLong(string? input) =>
        Policy(input, MatchKind.TooLong);

    public static Result BlockedCharacter(string? input, int index, string character) =>
        Policy(input, MatchKind.BlockedCharacter, index, character);

    public static Result LeadingSeparator(string? input, int index, string character) =>
        Policy(input, MatchKind.LeadingSeparator, index, character);

    public static Result TrailingSeparator(string? input, int index, string character) =>
        Policy(input, MatchKind.TrailingSeparator, index, character);

    private static Result Policy(
        string? input,
        MatchKind kind,
        int? index = null,
        string? character = null) =>
        new Result(true, input, null, null, kind, index, character, null, null);
}
