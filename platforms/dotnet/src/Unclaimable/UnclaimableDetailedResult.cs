namespace Unclaimable;

/// <summary>
/// Describes one policy or reserved-name diagnostic detected while evaluating an identifier.
/// </summary>
public sealed class UnclaimableDiagnostic
{
    /// <summary>
    /// Initializes a diagnostic describing one detected rejection reason.
    /// </summary>
    /// <param name="kind">The kind of match or policy violation.</param>
    /// <param name="matchedValue">The reserved dataset value that matched, when applicable.</param>
    /// <param name="category">The dataset category of the matched value, when applicable.</param>
    /// <param name="offendingCharacterIndex">The zero-based UTF-16 index of an offending character, when applicable.</param>
    /// <param name="offendingCharacter">The offending Unicode character, when applicable.</param>
    /// <param name="matchStartIndex">The zero-based start index of a reserved-name match, when available.</param>
    /// <param name="matchLength">The length of the reserved-name match, when available.</param>
    /// <param name="message">An optional human-readable diagnostic message.</param>
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

    /// <summary>Gets the kind of match or policy violation.</summary>
    public UnclaimableMatchKind Kind { get; }

    /// <summary>Gets the reserved dataset value that matched, when applicable.</summary>
    public string? MatchedValue { get; }

    /// <summary>Gets the dataset category of the matched value, when applicable.</summary>
    public string? Category { get; }

    /// <summary>Gets the zero-based UTF-16 index of the offending character, when applicable.</summary>
    public int? OffendingCharacterIndex { get; }

    /// <summary>Gets the offending Unicode character, when applicable.</summary>
    public string? OffendingCharacter { get; }

    /// <summary>Gets the zero-based start index of the reserved-name match, when available.</summary>
    public int? MatchStartIndex { get; }

    /// <summary>Gets the length of the reserved-name match, when available.</summary>
    public int? MatchLength { get; }

    /// <summary>Gets the optional human-readable diagnostic message.</summary>
    public string? Message { get; }
}

/// <summary>
/// Represents a complete validation result containing every diagnostic detected for an identifier.
/// </summary>
public sealed class UnclaimableDetailedResult
{
    /// <summary>
    /// Initializes a detailed validation result and snapshots the supplied diagnostics.
    /// </summary>
    /// <param name="input">The original identifier that was evaluated.</param>
    /// <param name="diagnostics">The diagnostics detected for the identifier.</param>
    public UnclaimableDetailedResult(string? input, IReadOnlyList<UnclaimableDiagnostic> diagnostics)
    {
        Input = input;
        Diagnostics = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
    }

    /// <summary>Gets the original identifier that was evaluated.</summary>
    public string? Input { get; }

    /// <summary>Gets the UTF-16 length of <see cref="Input"/>, or zero when the input is <see langword="null"/>.</summary>
    public int InputLength => Input?.Length ?? 0;

    /// <summary>Gets whether at least one rejection diagnostic was detected.</summary>
    public bool IsReserved => Diagnostics.Count > 0;

    /// <summary>Gets whether no rejection diagnostics were detected.</summary>
    public bool IsClaimable => !IsReserved;

    /// <summary>Gets an immutable snapshot of the detected diagnostics.</summary>
    public IReadOnlyList<UnclaimableDiagnostic> Diagnostics { get; }
}
