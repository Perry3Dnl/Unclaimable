namespace Unclaimable;

public sealed partial class Options
{
    private readonly HashSet<Category> _disabledCategories = new HashSet<Category>();
    private readonly List<ReservationRegistration> _reservations = new List<ReservationRegistration>();

    /// <summary>
    /// Complete identifiers that are allowed to bypass built-in reserved-name matches.
    /// Structural validation and explicit application reservations still apply.
    /// Values are captured when a <see cref="Checker"/> is constructed and use exact normalization:
    /// leading/trailing whitespace is trimmed, Unicode NFKC normalization is applied, and casing is lowered invariantly.
    /// </summary>
    public ICollection<string> AllowedIdentifiers { get; } = new List<string>();

    /// <summary>Built-in reserved-name categories disabled for this checker. All categories are enabled by default.</summary>
    public IReadOnlyCollection<Category> DisabledCategories => _disabledCategories;

    /// <summary>Disables one built-in reserved-name category while leaving every other category enabled.</summary>
    /// <param name="category">The category to disable.</param>
    /// <returns>This options instance.</returns>
    public Options DisableCategory(Category category)
    {
        ValidateCategory(category, nameof(category));
        _disabledCategories.Add(category);
        return this;
    }

    /// <summary>Re-enables a built-in reserved-name category that was previously disabled.</summary>
    /// <param name="category">The category to enable.</param>
    /// <returns>This options instance.</returns>
    public Options EnableCategory(Category category)
    {
        ValidateCategory(category, nameof(category));
        _disabledCategories.Remove(category);
        return this;
    }

    /// <summary>Adds an application-specific reserved identifier using the requested matching mode.</summary>
    /// <param name="value">The identifier to reserve.</param>
    /// <param name="matching">How the reservation participates in reserved-name matching.</param>
    /// <returns>This options instance.</returns>
    public Options Reserve(string value, ReservedMatchMode matching = ReservedMatchMode.Default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A reserved identifier cannot be null, empty, or whitespace.", nameof(value));
        }

        ValidateReservedMatchMode(matching, nameof(matching));
        _reservations.Add(new ReservationRegistration(value, matching));
        return this;
    }

    internal IReadOnlyList<ReservationRegistration> Reservations => _reservations;

    internal bool IsCategoryEnabled(string category)
    {
        foreach (var disabled in _disabledCategories)
        {
            if (string.Equals(GetDatasetCategoryName(disabled), category, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateCategory(Category category, string parameterName)
    {
        if (!Enum.IsDefined(typeof(Category), category))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Category must be a supported Category value.");
        }
    }

    private static void ValidateReservedMatchMode(ReservedMatchMode matching, string parameterName)
    {
        if (!Enum.IsDefined(typeof(ReservedMatchMode), matching))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Matching mode must be a supported ReservedMatchMode value.");
        }
    }

    private static string GetDatasetCategoryName(Category category)
    {
        switch (category)
        {
            case Category.Authentication:
                return "authentication";
            case Category.Automation:
                return "automation";
            case Category.Brands:
                return "brands";
            case Category.Commerce:
                return "commerce";
            case Category.Communications:
                return "communications";
            case Category.Community:
                return "community";
            case Category.Developer:
                return "developer";
            case Category.Finance:
                return "finance";
            case Category.Governance:
                return "governance";
            case Category.Identity:
                return "identity";
            case Category.Infrastructure:
                return "infrastructure";
            case Category.Legal:
                return "legal";
            case Category.Moderation:
                return "moderation";
            case Category.Official:
                return "official";
            case Category.Operations:
                return "operations";
            case Category.Other:
                return "other";
            case Category.Profanity:
                return "profanity";
            case Category.Roles:
                return "roles";
            case Category.Security:
                return "security";
            case Category.Support:
                return "support";
            case Category.System:
                return "system";
            case Category.Technology:
                return "technology";
            default:
                throw new ArgumentOutOfRangeException(nameof(category));
        }
    }

    internal sealed class ReservationRegistration
    {
        internal ReservationRegistration(string value, ReservedMatchMode matching)
        {
            Value = value;
            Matching = matching;
        }

        internal string Value { get; }
        internal ReservedMatchMode Matching { get; }
    }
}
