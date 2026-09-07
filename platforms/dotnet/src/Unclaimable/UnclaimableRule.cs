namespace Unclaimable;

/// <summary>
/// Describes the effective rules used by an <see cref="UnclaimableChecker"/>.
/// </summary>
[Flags]
public enum UnclaimableRule
{
    None = 0,
    Exact = 1 << 0,
    Compact = 1 << 1,
    Partial = 1 << 2,
    Obfuscation = 1 << 3,
    UnicodeConfusables = 1 << 4,
    Profanity = 1 << 5,
    ProfanityPartial = 1 << 6,
    RejectNumbers = 1 << 7,
    AsciiOnly = 1 << 8
}
