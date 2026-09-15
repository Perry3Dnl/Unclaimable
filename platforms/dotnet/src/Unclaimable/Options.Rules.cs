namespace Unclaimable;

public sealed partial class Options
{
    private const Rule OptionalRules = Rule.CountryNames | Rule.PopularCityNames | Rule.CelebrityNames;

    private const Rule AllRules =
        Rule.MinimumLength
        | Rule.MaximumLength
        | Rule.Whitespace
        | Rule.BlockedCharacters
        | Rule.LeadingSeparator
        | Rule.TrailingSeparator
        | Rule.Numbers
        | Rule.CompactMatching
        | Rule.PartialMatching
        | Rule.Profanity
        | Rule.ObfuscationMatching
        | Rule.UnicodeConfusableMatching
        | Rule.CountryNames
        | Rule.PopularCityNames
        | Rule.CelebrityNames;

    private Rule _enabledOptionalRules = Rule.None;

    /// <summary>
    /// Gets opt-in rules currently enabled for newly constructed checkers.
    /// Country-name, popular-city-name, and celebrity-name matching are disabled by default.
    /// </summary>
    public Rule EnabledOptionalRules => _enabledOptionalRules & ~DisabledRules;

    /// <summary>
    /// Enables one or more rules without changing unrelated rule configuration.
    /// Existing rules are enabled by clearing them from <see cref="DisabledRules"/>.
    /// Opt-in rules such as <see cref="Rule.CountryNames"/>, <see cref="Rule.PopularCityNames"/>,
    /// and <see cref="Rule.CelebrityNames"/> are activated explicitly by this method.
    /// </summary>
    /// <param name="rule">One rule or a bitwise combination of supported rules.</param>
    /// <returns>This options instance.</returns>
    public Options EnableRule(Rule rule)
    {
        ValidateRule(rule, nameof(rule));
        DisabledRules &= ~rule;
        _enabledOptionalRules |= rule & OptionalRules;
        return this;
    }

    /// <summary>Disables one or more rules without changing unrelated rule configuration.</summary>
    /// <param name="rule">One rule or a bitwise combination of supported rules.</param>
    /// <returns>This options instance.</returns>
    public Options DisableRule(Rule rule)
    {
        ValidateRule(rule, nameof(rule));
        DisabledRules |= rule;
        _enabledOptionalRules &= ~(rule & OptionalRules);
        return this;
    }

    internal bool IsOptionalRuleEnabled(Rule rule) =>
        (_enabledOptionalRules & rule) == rule && (DisabledRules & rule) == 0;

    internal IReadOnlyList<ReservationRegistration> BuildEffectiveReservations()
    {
        if (!IsOptionalRuleEnabled(Rule.CountryNames)
            && !IsOptionalRuleEnabled(Rule.PopularCityNames)
            && !IsOptionalRuleEnabled(Rule.CelebrityNames))
        {
            return _reservations;
        }

        var effective = new List<ReservationRegistration>(_reservations);

        if (IsOptionalRuleEnabled(Rule.CountryNames))
        {
            foreach (var value in GeographyData.CountryNames)
            {
                effective.Add(new ReservationRegistration(
                    GeographyData.CountryReservationPrefix + value,
                    ReservedMatchMode.Exact));
            }
        }

        if (IsOptionalRuleEnabled(Rule.PopularCityNames))
        {
            foreach (var value in GeographyData.PopularCityNames)
            {
                effective.Add(new ReservationRegistration(
                    GeographyData.CityReservationPrefix + value,
                    ReservedMatchMode.Exact));
            }
        }

        if (IsOptionalRuleEnabled(Rule.CelebrityNames))
        {
            foreach (var value in CelebrityData.Names)
            {
                effective.Add(new ReservationRegistration(
                    CelebrityData.ReservationPrefix + value,
                    ReservedMatchMode.Exact));
            }
        }

        return effective;
    }

    private static void ValidateRule(Rule rule, string parameterName)
    {
        if ((rule & ~AllRules) != 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Rule must contain only supported Rule values.");
        }
    }
}
