namespace Unclaimable;

/// <summary>Controls the optional guard for one-sided short slang/profanity substring matches.</summary>
public enum FalsePositiveSensitivity
{
    /// <summary>Preserves existing matching behavior. No slang matches are suppressed.</summary>
    Low = 0,

    /// <summary>Allows one-sided slang fragments up to four characters long.</summary>
    Medium = 1,

    /// <summary>Allows one-sided slang fragments up to six characters long.</summary>
    High = 2
}
