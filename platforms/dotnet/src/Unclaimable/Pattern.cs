namespace Unclaimable;

/// <summary>
/// Higher-level identifier-shape protections that can reject suspicious or degenerate patterns.
/// Pattern checks are independent: passing or disabling one pattern never bypasses other patterns,
/// structural rules, or reserved-name matching.
/// </summary>
[Flags]
public enum Pattern
{
    /// <summary>No pattern.</summary>
    None = 0,

    /// <summary>Reject identifiers made entirely of Unicode decimal digits.</summary>
    NumericOnly = 1 << 0,

    /// <summary>Reject identifiers formed by repeating a short unit multiple times.</summary>
    Repeated = 1 << 1,

    /// <summary>Reject identifiers that contain no Unicode letters or numbers.</summary>
    SymbolOnly = 1 << 2,

    /// <summary>Reject a conservative set of known ASCII-art constructions.</summary>
    AsciiArt = 1 << 3,

    /// <summary>Reject identifiers whose cased letters are all uppercase.</summary>
    UppercaseOnly = 1 << 4
}
