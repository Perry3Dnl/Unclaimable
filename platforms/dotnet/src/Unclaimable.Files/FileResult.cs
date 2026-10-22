namespace Unclaimable.Files;

/// <summary>Describes a filename-policy decision. Acceptance does not establish content safety or filesystem uniqueness.</summary>
public sealed class FileResult
{
    internal FileResult(string? fileName, FileFailureKind failureKind, global::Unclaimable.Result? coreResult = null)
    {
        FileName = fileName;
        FailureKind = failureKind;
        CoreResult = coreResult;
    }

    /// <summary>Gets the original filename without normalization or renaming.</summary>
    public string? FileName { get; }

    /// <summary>Gets the first rejection reason, or None.</summary>
    public FileFailureKind FailureKind { get; }

    /// <summary>Gets whether all configured checks passed.</summary>
    public bool IsAllowed => FailureKind == FileFailureKind.None;

    /// <summary>Gets whether the filename is claimable under the configured file policy.</summary>
    public bool IsClaimable => IsAllowed;

    /// <summary>
    /// Gets the underlying Core result for a device-name or name-policy rejection, or the accepted full-name result.
    /// Its input and offsets refer to the value Core checked: the full filename, stem, or device-name component.
    /// </summary>
    public global::Unclaimable.Result? CoreResult { get; }
}
