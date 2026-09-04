namespace Unclaimable;

public sealed class UnclaimableDiagnostic
{
    public UnclaimableDiagnostic(
        UnclaimableMatchKind kind,
        string? matchedValue = null,
        string? category = null,
        int? offendingCharacterIndex = null,
        string? offendingCharacter = null,
        int? matchStartIndex = null,
        int? matchLength = null,
        string? message = null)
    {
        Kind = kind;
        MatchedValue = matchedValue;
        Category = category;
        OffendingCharacterIndex = offendingCharacterIndex;
        OffendingCharacter = offendingCharacter;
        MatchStartIndex = matchStartIndex;
        MatchLength = matchLength;
        Message = message;
    }

    public UnclaimableMatchKind Kind { get; }

    public string? MatchedValue { get; }

    public string? Category { get; }

    public int? OffendingCharacterIndex { get; }

    public string? OffendingCharacter { get; }

    public int? MatchStartIndex { get; }

    public int? MatchLength { get; }

    public string? Message { get; }
}

public sealed class UnclaimableDetailedResult
{
    public UnclaimableDetailedResult(string? input, IReadOnlyList<UnclaimableDiagnostic> diagnostics)
    {
        Input = input;
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public string? Input { get; }

    public int InputLength => Input?.Length ?? 0;

    public bool IsReserved => Diagnostics.Count > 0;

    public bool IsClaimable => !IsReserved;

    public IReadOnlyList<UnclaimableDiagnostic> Diagnostics { get; }
}
