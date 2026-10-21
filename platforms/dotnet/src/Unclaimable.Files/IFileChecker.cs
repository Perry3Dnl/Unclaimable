namespace Unclaimable.Files;

/// <summary>Checks individual filenames against a captured file policy.</summary>
public interface IFileChecker
{
    /// <summary>Returns the first rejection or an accepted result. Null filenames are rejected.</summary>
    FileResult Check(string? fileName);

    /// <summary>Returns whether the filename passes all configured checks.</summary>
    bool IsAllowed(string? fileName);

    /// <summary>Returns whether the filename is claimable under the configured file policy.</summary>
    bool IsClaimable(string? fileName);
}
