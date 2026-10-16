namespace Unclaimable.Files;

/// <summary>Configures filename checks. Values are captured when a <see cref="FileChecker"/> is constructed.</summary>
public sealed class FileCheckerOptions
{
    /// <summary>Creates filename-adapted Core options with optional content protection disabled.</summary>
    public FileCheckerOptions()
    {
        NameOptions = CreateNameOptions();
    }

    /// <summary>Gets or sets the target naming profile. Defaults to Portable.</summary>
    public FileProfile Profile { get; set; } = FileProfile.Portable;

    /// <summary>Gets or sets the maximum filename length in UTF-16 code units. Defaults to 255.</summary>
    public int MaximumLength { get; set; } = 255;

    /// <summary>Gets or sets the UTF-8 byte limit for Portable and Linux profiles. Defaults to 255; Windows ignores it.</summary>
    public int MaximumUtf8Bytes { get; set; } = 255;

    /// <summary>Gets allowed extension suffixes, such as .pdf or .tar.gz. An empty set allows extensionless names too.</summary>
    public ISet<string> AllowedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets blocked extension suffixes. A block wins over an allow; the default set is empty.</summary>
    public ISet<string> BlockedExtensions { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets whether blocked extensions also match before another dot, such as .exe in invoice.exe.pdf. Defaults to true.</summary>
    public bool BlockInnerExtensions { get; set; } = true;

    /// <summary>
    /// Gets the Core options used for full-filename and final-extension-free stem checks.
    /// Built-in categories, username rules, and shape patterns start disabled; Core Unicode safety remains enabled.
    /// Use Reserve for application names, or explicitly enable categories and matching rules for content protection.
    /// </summary>
    public global::Unclaimable.Options NameOptions { get; }

    internal static global::Unclaimable.Options CreateNameOptions()
    {
        var options = new global::Unclaimable.Options { Strictness = global::Unclaimable.Strictness.Standard };
        foreach (global::Unclaimable.Rule rule in Enum.GetValues(typeof(global::Unclaimable.Rule)))
            options.DisableRule(rule);
        foreach (global::Unclaimable.Pattern pattern in Enum.GetValues(typeof(global::Unclaimable.Pattern)))
            options.DisablePattern(pattern);
        foreach (global::Unclaimable.Category category in Enum.GetValues(typeof(global::Unclaimable.Category)))
            options.DisableCategory(category);
        return options;
    }
}
