namespace Unclaimable;

public sealed partial class Options
{
    private const Rule OptionalRules =
        Rule.CountryNames
        | Rule.PopularCityNames
        | Rule.CelebrityNames
        | Rule.Nationalities
        | Rule.Currencies
        | Rule.Religions
        | Rule.Landmarks
        | Rule.Events
        | Rule.Awards
        | Rule.FictionalCharacters
        | Rule.Franchises
        | Rule.Professions
        | Rule.Military;

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
        | OptionalRules;

    private Rule _enabledOptionalRules = OptionalRules;

    /// <summary>
    /// Gets named identity-list rules currently enabled for newly constructed checkers.
    /// All built-in rules are enabled by default in 0.8.0 and later unless explicitly disabled.
    /// </summary>
    public Rule EnabledOptionalRules => _enabledOptionalRules & ~DisabledRules;

    /// <summary>
    /// Enables one or more rules without changing unrelated rule configuration.
    /// Existing rules are enabled by clearing them from <see cref="DisabledRules"/>.
    /// Named identity-list rules can be re-enabled by this method after being disabled.
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
        if ((_enabledOptionalRules & OptionalRules & ~DisabledRules) == Rule.None)
        {
            return _reservations;
        }

        var effective = new List<ReservationRegistration>(_reservations);

        AddOptionalReservations(effective, Rule.CountryNames, GeographyData.CountryReservationPrefix, GeographyData.CountryNames);
        AddOptionalReservations(effective, Rule.PopularCityNames, GeographyData.CityReservationPrefix, GeographyData.PopularCityNames);
        AddOptionalReservations(effective, Rule.CelebrityNames, CelebrityData.ReservationPrefix, CelebrityData.Names);
        AddOptionalReservations(effective, Rule.Nationalities, OptionalIdentityData.NationalityPrefix, OptionalIdentityData.Nationalities);
        AddOptionalReservations(effective, Rule.Currencies, OptionalIdentityData.CurrencyPrefix, OptionalIdentityData.Currencies);
        AddOptionalReservations(effective, Rule.Religions, OptionalIdentityData.ReligionPrefix, OptionalIdentityData.Religions);
        AddOptionalReservations(effective, Rule.Landmarks, OptionalIdentityData.LandmarkPrefix, OptionalIdentityData.Landmarks);
        AddOptionalReservations(effective, Rule.Events, OptionalIdentityData.EventPrefix, OptionalIdentityData.Events);
        AddOptionalReservations(effective, Rule.Awards, OptionalIdentityData.AwardPrefix, OptionalIdentityData.Awards);
        AddOptionalReservations(effective, Rule.FictionalCharacters, OptionalIdentityData.FictionalCharacterPrefix, OptionalIdentityData.FictionalCharacters);
        AddOptionalReservations(effective, Rule.Franchises, OptionalIdentityData.FranchisePrefix, OptionalIdentityData.Franchises);
        AddOptionalReservations(effective, Rule.Professions, OptionalIdentityData.ProfessionPrefix, OptionalIdentityData.Professions);
        AddOptionalReservations(effective, Rule.Military, OptionalIdentityData.MilitaryPrefix, OptionalIdentityData.Military);

        return effective;
    }

    private void AddOptionalReservations(
        List<ReservationRegistration> effective,
        Rule rule,
        string prefix,
        IEnumerable<string> values)
    {
        if (!IsOptionalRuleEnabled(rule))
        {
            return;
        }

        foreach (var value in values)
        {
            effective.Add(new ReservationRegistration(prefix + value, "custom", ReservedMatchMode.Exact));
        }
    }

    private static void ValidateRule(Rule rule, string parameterName)
    {
        if ((rule & ~AllRules) != 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Rule must contain only supported Rule values.");
        }
    }
}
