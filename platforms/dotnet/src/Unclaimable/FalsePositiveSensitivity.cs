namespace Unclaimable;

/// <summary>Controls the optional guard for one-sided short slang/profanity substring matches.</summary>
public enum FalsePositiveSensitivity
{
    /// <summary>Preserves existing matching behavior. No slang matches are suppressed.</summary>
    Low = 0,

    /// <summary>Relaxes edge-only slang fragments up to four characters with at least four adjoining characters.</summary>
    Medium = 1,

    /// <summary>Relaxes edge-only slang fragments up to six characters with at least four adjoining characters.</summary>
    High = 2
}
