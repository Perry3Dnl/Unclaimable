namespace Unclaimable;

public sealed partial class Options
{
    private const Rule OptionalRules = Rule.CountryNames | Rule.PopularCityNames;

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
        | Rule.PopularCityNames;

    private Rule _enabledOptionalRules = Rule.None;

    /// <summary>
    /// Gets opt-in rules explicitly enabled for newly constructed checkers.
    /// Country-name and popular-city-name matching are disabled by default.
    /// </summary>
    public Rule EnabledOptionalRules => _enabledOptionalRules;

    /// <summary>
    /// Enables one or more rules without changing unrelated rule configuration.
    /// Existing rules are enabled by clearing them from <see cref="DisabledRules"/>.
    /// Opt-in rules such as <see cref="Rule.CountryNames"/> and <see cref="Rule.PopularCityNames"/>
    /// are activated explicitly by this method.
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
        _enabledOptionalRules &= ~rule;
        return this;
    }

    internal bool IsOptionalRuleEnabled(Rule rule) =>
        (_enabledOptionalRules & rule) == rule && (DisabledRules & rule) == 0;

    private static void ValidateRule(Rule rule, string parameterName)
    {
        if ((rule & ~AllRules) != 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Rule must contain only supported Rule values.");
        }
    }
}
