namespace Unclaimable;

public enum UnclaimableMatchKind
{
    None = 0,
    Exact = 1,
    Compact = 2
}

public sealed record UnclaimableResult(
    bool IsReserved,
    string? Input,
    string? MatchedValue,
    string? Category,
    UnclaimableMatchKind MatchKind)
{
    public static UnclaimableResult Allowed(string? input) =>
        new(false, input, null, null, UnclaimableMatchKind.None);
}
