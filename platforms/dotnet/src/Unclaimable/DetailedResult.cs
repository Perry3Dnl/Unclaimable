namespace Unclaimable;

/// <summary>
/// Describes one validation or matching diagnostic. Lengths and indexes use UTF-16 code units.
/// </summary>
public sealed class Diagnostic
{
    /// <summary>
    /// Creates a diagnostic using the legacy transformed-text match offsets.
    /// </summary>
    public Diagnostic(
        MatchKind kind,
        string? matchedValue = null,
        string? category = null,
        int? offendingCharacterIndex = null,
        string? offendingCharacter = null,
        int? matchStartIndex = null,
        int? matchLength = null,
        string? message = null)
        : this(
            kind,
            matchedValue,
            category,
            offendingCharacterIndex,
            offendingCharacter,
            matchStartIndex,
            matchLength,
            message,
            null,
            null)
    {
    }

    internal Diagnostic(
        MatchKind kind,
        string? matchedValue,
        string? category,
        int? offendingCharacterIndex,
        string? offendingCharacter,
        int? matchStartIndex,
        int? matchLength,
        string? message,
        int? originalMatchStartIndex,
        int? originalMatchLength)
    {
        Kind = kind;
        MatchedValue = matchedValue;
        Category = category;
        OffendingCharacterIndex = offendingCharacterIndex;
        OffendingCharacter = offendingCharacter;
        MatchStartIndex = matchStartIndex;
        MatchLength = matchLength;
        Message = message;
        OriginalMatchStartIndex = originalMatchStartIndex;
        OriginalMatchLength = originalMatchLength;
    }

    /// <summary>Gets the diagnostic reason.</summary>
    public MatchKind Kind { get; }

    /// <summary>Gets the matched reserved value, when applicable.</summary>
    public string? MatchedValue { get; }

    /// <summary>Gets the matched dataset category, when applicable.</summary>
    public string? Category { get; }

    /// <summary>Gets an offending character index in the original input, when applicable.</summary>
    public int? OffendingCharacterIndex { get; }

    /// <summary>Gets the offending Unicode scalar value, when applicable.</summary>
    public string? OffendingCharacter { get; }

    /// <summary>Gets the legacy transformed-text match start offset.</summary>
    public int? MatchStartIndex { get; }

    /// <summary>Gets the legacy transformed-text match length.</summary>
    public int? MatchLength { get; }

    /// <summary>Start of the matched span in the original UTF-16 input.</summary>
    public int? OriginalMatchStartIndex { get; }

    /// <summary>Length of the matched span in the original UTF-16 input.</summary>
    public int? OriginalMatchLength { get; }

    /// <summary>Gets the optional user-facing diagnostic message.</summary>
    public string? Message { get; }
}

/// <summary>Contains all diagnostics collected for one input value.</summary>
public sealed class DetailedResult
{
    /// <summary>Creates a detailed validation result.</summary>
    public DetailedResult(string? input, IReadOnlyList<Diagnostic> diagnostics)
    {
        Input = input;
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    /// <summary>Gets the original input value.</summary>
    public string? Input { get; }

    /// <summary>Gets the input length in UTF-16 code units.</summary>
    public int InputLength => Input?.Length ?? 0;

    /// <summary>
    /// Gets whether any diagnostic rejected the value. Structural validation failures are included.
    /// </summary>
    public bool IsReserved => Diagnostics.Count > 0;

    /// <summary>Gets whether no diagnostics rejected the value.</summary>
    public bool IsClaimable => !IsReserved;

    /// <summary>Gets all collected diagnostics.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
