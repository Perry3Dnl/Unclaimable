using System.Text;

namespace Unclaimable.Extended;

/// <summary>Controls which Extended dataset groups are registered.</summary>
public sealed class ExtendedOptions
{
    private readonly HashSet<ExtendedCategory> _disabledCategories = new HashSet<ExtendedCategory>();

    /// <summary>Dataset groups disabled for this registration. All groups are enabled by default.</summary>
    public IReadOnlyCollection<ExtendedCategory> DisabledCategories => _disabledCategories;

    /// <summary>Complete Extended identifiers to omit from this registration.</summary>
    public ISet<string> AllowedIdentifiers { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Disables one Extended dataset group.</summary>
    public ExtendedOptions DisableCategory(ExtendedCategory category)
    {
        ValidateCategory(category, nameof(category));
        _disabledCategories.Add(category);
        return this;
    }

    /// <summary>Re-enables one Extended dataset group.</summary>
    public ExtendedOptions EnableCategory(ExtendedCategory category)
    {
        ValidateCategory(category, nameof(category));
        _disabledCategories.Remove(category);
        return this;
    }

    internal bool IsEnabled(ExtendedCategory category) => !_disabledCategories.Contains(category);

    internal bool IsAllowed(string value)
    {
        var normalized = Normalize(value);
        return AllowedIdentifiers.Any(allowed =>
            string.Equals(Normalize(allowed), normalized, StringComparison.Ordinal));
    }

    private static string Normalize(string value) =>
        value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();

    private static void ValidateCategory(ExtendedCategory category, string parameterName)
    {
        if (!Enum.IsDefined(typeof(ExtendedCategory), category))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Category must be a supported ExtendedCategory value.");
        }
    }
}
