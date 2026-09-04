namespace Unclaimable;

public enum UnclaimableMatchKind
{
    None = 0,
    Exact = 1,
    Compact = 2,
    Obfuscated = 3
}

public sealed class UnclaimableResult
{
    public UnclaimableResult(
        bool isReserved,
        string? input,
        string? matchedValue,
        string? category,
        UnclaimableMatchKind matchKind)
    {
        IsReserved = isReserved;
        Input = input;
        MatchedValue = matchedValue;
        Category = category;
        MatchKind = matchKind;
    }

    public bool IsReserved { get; }

    public string? Input { get; }

    public string? MatchedValue { get; }

    public string? Category { get; }

    public UnclaimableMatchKind MatchKind { get; }

    public static UnclaimableResult Allowed(string? input) =>
        new UnclaimableResult(false, input, null, null, UnclaimableMatchKind.None);
}
