namespace Unclaimable.Files;

/// <summary>Chooses filename restrictions independently of the operating system running the checker.</summary>
public enum FileProfile
{
    /// <summary>Combines Windows naming restrictions with a UTF-8 byte limit for Linux portability.</summary>
    Portable = 0,
    /// <summary>Applies Windows invalid-character, device-name, and trailing-dot/space restrictions.</summary>
    Windows = 1,
    /// <summary>Allows Linux filename characters except slash and NUL; Core Unicode policy still applies.</summary>
    Linux = 2
}
