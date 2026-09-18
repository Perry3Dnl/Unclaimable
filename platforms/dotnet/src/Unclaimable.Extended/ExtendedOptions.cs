namespace Unclaimable.Extended;

/// <summary>Controls which Unclaimable.Extended dataset groups are registered.</summary>
public sealed class ExtendedOptions
{
    private readonly HashSet<ExtendedCategory> _disabledCategories = new HashSet<ExtendedCategory>();

    /// <summary>Gets Extended categories disabled for this registration. All are enabled by default.</summary>
    public IReadOnlyCollection<ExtendedCategory> DisabledCategories => _disabledCategories;

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

    internal bool IsEnabled(string category)
    {
        return !_disabledCategories.Contains(MapCategory(category));
    }

    internal static ExtendedCategory MapCategory(string category)
    {
        switch (category)
        {
            case "brands": return ExtendedCategory.CompaniesAndBrands;
            case "regionalbrands": return ExtendedCategory.RegionalBrands;
            case "financial": return ExtendedCategory.FinancialInstitutions;
            case "government": return ExtendedCategory.GovernmentBodies;
            case "internationalorganizations": return ExtendedCategory.InternationalOrganizations;
            case "sports": return ExtendedCategory.Sports;
            case "education": return ExtendedCategory.Education;
            case "media": return ExtendedCategory.Media;
            case "transport": return ExtendedCategory.Transport;
            case "healthcare": return ExtendedCategory.Healthcare;
            case "historicalfigures": return ExtendedCategory.HistoricalFigures;
            case "publicfigures": return ExtendedCategory.PublicFigures;
            case "celebrities": return ExtendedCategory.Celebrities;
            case "fiction": return ExtendedCategory.Fiction;
            case "entertainment": return ExtendedCategory.Entertainment;
            case "professions": return ExtendedCategory.Professions;
            case "multilingual": return ExtendedCategory.MultilingualReserved;
            case "regionalprofanity": return ExtendedCategory.RegionalProfanity;
            case "crypto": return ExtendedCategory.Crypto;
            case "webservices": return ExtendedCategory.WebServices;
            case "geography": return ExtendedCategory.Geography;
            default:
                throw new InvalidOperationException($"Unknown Unclaimable.Extended category '{category}'.");
        }
    }

    private static void ValidateCategory(ExtendedCategory category, string parameterName)
    {
        if (!Enum.IsDefined(typeof(ExtendedCategory), category))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Category must be a supported ExtendedCategory value.");
        }
    }
}
