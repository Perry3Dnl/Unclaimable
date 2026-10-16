using System.Text;

namespace Unclaimable.Files;

/// <summary>Applies filename syntax rules and delegates name matching and Unicode validation to Unclaimable Core.</summary>
public sealed class FileChecker : IFileChecker
{
    private static readonly Lazy<global::Unclaimable.Checker> DeviceNames =
        new Lazy<global::Unclaimable.Checker>(CreateDeviceNameChecker);
    private readonly global::Unclaimable.IChecker _nameChecker;
    private readonly FileProfile _profile;
    private readonly int _maximumLength;
    private readonly int _maximumUtf8Bytes;
    private readonly bool _blockInnerExtensions;
    private readonly string[] _allowedExtensions;
    private readonly string[] _blockedExtensions;

    /// <summary>Creates a checker with portable filename defaults.</summary>
    public FileChecker() : this(new FileCheckerOptions()) { }

    /// <summary>Captures filename settings and constructs the underlying Core name checker.</summary>
    public FileChecker(FileCheckerOptions options)
        : this(options is null ? throw new ArgumentNullException(nameof(options))
            : new global::Unclaimable.Checker(options.NameOptions), options) { }

    /// <summary>Captures filename settings and uses a supplied Core checker for full-name and stem decisions.</summary>
    public FileChecker(global::Unclaimable.IChecker nameChecker, FileCheckerOptions options)
    {
        _nameChecker = nameChecker ?? throw new ArgumentNullException(nameof(nameChecker));
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (!Enum.IsDefined(typeof(FileProfile), options.Profile))
            throw new ArgumentOutOfRangeException(nameof(options.Profile));
        if (options.MaximumLength < 1) throw new ArgumentOutOfRangeException(nameof(options.MaximumLength));
        if (options.MaximumUtf8Bytes < 1) throw new ArgumentOutOfRangeException(nameof(options.MaximumUtf8Bytes));

        _profile = options.Profile;
        _maximumLength = options.MaximumLength;
        _maximumUtf8Bytes = options.MaximumUtf8Bytes;
        _blockInnerExtensions = options.BlockInnerExtensions;
        _allowedExtensions = CaptureExtensions(options.AllowedExtensions, nameof(options.AllowedExtensions));
        _blockedExtensions = CaptureExtensions(options.BlockedExtensions, nameof(options.BlockedExtensions));
    }

    /// <inheritdoc />
    public bool IsAllowed(string? fileName) => Check(fileName).IsAllowed;

    /// <inheritdoc />
    public bool IsClaimable(string? fileName) => IsAllowed(fileName);

    /// <inheritdoc />
    public FileResult Check(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return new FileResult(fileName, FileFailureKind.MissingName);
        if (fileName == "." || fileName == "..") return new FileResult(fileName, FileFailureKind.DotSegment);
        if (fileName!.Length > _maximumLength
            || (_profile != FileProfile.Windows && Encoding.UTF8.GetByteCount(fileName) > _maximumUtf8Bytes))
            return new FileResult(fileName, FileFailureKind.TooLong);

        foreach (var character in fileName)
        {
            if (character == '\0' || character == '/'
                || (_profile != FileProfile.Linux && (character < 32 || "<>:\"\\|?*".IndexOf(character) >= 0)))
                return new FileResult(fileName, FileFailureKind.InvalidCharacter);
        }

        if (_profile != FileProfile.Linux)
        {
            var last = fileName[fileName.Length - 1];
            if (last == '.' || last == ' ') return new FileResult(fileName, FileFailureKind.TrailingDotOrSpace);
            var firstDot = fileName.IndexOf('.');
            var component = (firstDot < 0 ? fileName : fileName.Substring(0, firstDot)).TrimEnd(' ');
            // Device names are exact Core reservations; application allowances cannot bypass platform restrictions.
            var deviceResult = DeviceNames.Value.Check(component);
            if (deviceResult.IsReserved && deviceResult.Category == "file-device")
                return new FileResult(fileName, FileFailureKind.ReservedDeviceName, deviceResult);
        }

        foreach (var extension in _blockedExtensions)
        {
            if (HasExtension(fileName, extension)
                || (_blockInnerExtensions && fileName.IndexOf(extension + ".", StringComparison.OrdinalIgnoreCase) >= 0))
                return new FileResult(fileName, FileFailureKind.BlockedExtension);
        }

        if (_allowedExtensions.Length > 0 && !Array.Exists(_allowedExtensions, extension => HasExtension(fileName, extension)))
            return new FileResult(fileName, FileFailureKind.ExtensionNotAllowed);

        var fullNameResult = _nameChecker.Check(fileName);
        if (fullNameResult.IsReserved) return new FileResult(fileName, FileFailureKind.NamePolicy, fullNameResult);

        var lastDot = fileName.LastIndexOf('.');
        if (lastDot > 0)
        {
            var stemResult = _nameChecker.Check(fileName.Substring(0, lastDot));
            if (stemResult.IsReserved) return new FileResult(fileName, FileFailureKind.NamePolicy, stemResult);
        }
        return new FileResult(fileName, FileFailureKind.None, fullNameResult);
    }

    private static bool HasExtension(string fileName, string extension) =>
        fileName.Length > extension.Length && fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

    private static string[] CaptureExtensions(IEnumerable<string> extensions, string parameterName)
    {
        var normalized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions)
        {
            if (string.IsNullOrWhiteSpace(extension)) throw new ArgumentException("Extensions cannot be empty.", parameterName);
            var value = extension[0] == '.' ? extension : "." + extension;
            if (value.Length < 2 || value.EndsWith(".", StringComparison.Ordinal)
                || value.IndexOf("..", StringComparison.Ordinal) >= 0)
                throw new ArgumentException("Extensions must contain nonempty dot-separated segments.", parameterName);
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character) || char.IsSurrogate(character)
                    || "<>:\"/\\|?*".IndexOf(character) >= 0)
                    throw new ArgumentException("Extensions cannot contain whitespace, controls, surrogates, or path characters.", parameterName);
            }
            normalized.Add(value);
        }
        var result = new string[normalized.Count];
        normalized.CopyTo(result);
        return result;
    }

    private static global::Unclaimable.Checker CreateDeviceNameChecker()
    {
        var options = FileCheckerOptions.CreateNameOptions();
        foreach (var name in new[] { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$" })
            options.Reserve(name, "file-device", global::Unclaimable.ReservedMatchMode.Exact);
        for (var index = 1; index <= 9; index++)
        {
            options.Reserve("COM" + index, "file-device", global::Unclaimable.ReservedMatchMode.Exact);
            options.Reserve("LPT" + index, "file-device", global::Unclaimable.ReservedMatchMode.Exact);
        }
        return new global::Unclaimable.Checker(options);
    }
}
