namespace Unclaimable.Files;

/// <summary>Describes the first reason a filename was rejected.</summary>
public enum FileFailureKind
{
    /// <summary>All configured checks passed.</summary>
    None = 0,
    /// <summary>The filename was null, empty, or whitespace-only.</summary>
    MissingName = 1,
    /// <summary>The filename was the current-directory or parent-directory marker.</summary>
    DotSegment = 2,
    /// <summary>The filename exceeded a configured UTF-16 or UTF-8 length limit.</summary>
    TooLong = 3,
    /// <summary>The filename contained a character forbidden by the selected platform profile.</summary>
    InvalidCharacter = 4,
    /// <summary>A Windows or portable filename ended with an ASCII space or dot.</summary>
    TrailingDotOrSpace = 5,
    /// <summary>The name before its first dot matched a Windows device-name reservation in Core.</summary>
    ReservedDeviceName = 6,
    /// <summary>A configured blocked extension matched.</summary>
    BlockedExtension = 7,
    /// <summary>No configured allowed extension matched.</summary>
    ExtensionNotAllowed = 8,
    /// <summary>The Unclaimable name checker rejected the full filename or its stem.</summary>
    NamePolicy = 9
}
