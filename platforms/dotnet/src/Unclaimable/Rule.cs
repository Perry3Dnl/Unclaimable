namespace Unclaimable;

/// <summary>
/// Built-in validation rules that can be disabled for applications that need a more permissive policy.
/// All rules are enabled by default unless explicitly disabled.
/// </summary>
[Flags]
public enum Rule
{
    None = 0,
    MinimumLength = 1 << 0,
    MaximumLength = 1 << 1,
    Whitespace = 1 << 2,
    BlockedCharacters = 1 << 3,
    LeadingSeparator = 1 << 4,
    TrailingSeparator = 1 << 5,
    Numbers = 1 << 6,
    CompactMatching = 1 << 7,
    PartialMatching = 1 << 8,
    Profanity = 1 << 9,
    ObfuscationMatching = 1 << 10,
    UnicodeConfusableMatching = 1 << 11
}
