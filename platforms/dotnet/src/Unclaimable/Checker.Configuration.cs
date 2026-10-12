namespace Unclaimable;

public sealed partial class Checker
{
    private static readonly IReadOnlyList<PartialEntry> NoPartialEntries = Array.Empty<PartialEntry>();

    private readonly HashSet<string> _allowedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _exactOnlyCustom = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _customExact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _customCompact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly List<PartialEntry> _customPartialEntries = new List<PartialEntry>();
    private readonly Dictionary<string, string> _countryRuleExact = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _countryRuleCompact = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _cityRuleExact = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _cityRuleCompact = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _identityRuleExact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _identityRuleCompact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);

    private void CaptureOptionalRuleReservations(Options options)
    {
        AddOptionalGeographyRuleValues(
            options,
            Rule.CountryNames,
            GeographyData.CountryNames,
            _countryRuleExact,
            _countryRuleCompact);
        AddOptionalGeographyRuleValues(
            options,
            Rule.PopularCityNames,
            GeographyData.PopularCityNames,
            _cityRuleExact,
            _cityRuleCompact);

        AddOptionalIdentityRuleValues(options, Rule.CelebrityNames, CelebrityData.Names, "celebrity");
        AddOptionalIdentityRuleValues(options, Rule.Nationalities, OptionalIdentityData.Nationalities, "nationality");
        AddOptionalIdentityRuleValues(options, Rule.Currencies, OptionalIdentityData.Currencies, "currency");
        AddOptionalIdentityRuleValues(options, Rule.Religions, OptionalIdentityData.Religions, "religion");
        AddOptionalIdentityRuleValues(options, Rule.Landmarks, OptionalIdentityData.Landmarks, "landmark");
        AddOptionalIdentityRuleValues(options, Rule.Events, OptionalIdentityData.Events, "event");
        AddOptionalIdentityRuleValues(options, Rule.Awards, OptionalIdentityData.Awards, "award");
        AddOptionalIdentityRuleValues(
            options,
            Rule.FictionalCharacters,
            OptionalIdentityData.FictionalCharacters,
            "fictionalcharacter");
        AddOptionalIdentityRuleValues(options, Rule.Franchises, OptionalIdentityData.Franchises, "franchise");
        AddOptionalIdentityRuleValues(options, Rule.Professions, OptionalIdentityData.Professions, "profession");
        AddOptionalIdentityRuleValues(options, Rule.Military, OptionalIdentityData.Military, "military");
    }

    private static void AddOptionalGeographyRuleValues(
        Options options,
        Rule rule,
        string[] values,
        Dictionary<string, string> exactValues,
        Dictionary<string, string> compactValues)
    {
        if (!options.IsOptionalRuleEnabled(rule))
        {
            return;
        }

        foreach (var value in values)
        {
            if (!options.IncludeHighCollisionIdentityTerms
                && OptionalIdentityData.IsHighCollisionValue(value, rule))
            {
                continue;
            }

            AddGeographyRuleValue(value, exactValues, compactValues);
        }
    }

    private void AddOptionalIdentityRuleValues(
        Options options,
        Rule rule,
        string[] values,
        string category)
    {
        if (!options.IsOptionalRuleEnabled(rule))
        {
            return;
        }

        foreach (var value in values)
        {
            if (!options.IncludeHighCollisionIdentityTerms
                && OptionalIdentityData.IsHighCollisionValue(value, rule))
            {
                continue;
            }

            AddIdentityRuleValue(value, category);
        }
    }

    private void CaptureAllowedIdentifiers(Options options)
    {
        foreach (var value in options.AllowedIdentifiers)
        {
            var exact = NormalizeExact(value);
            if (exact is not null)
            {
                _allowedIdentifiers.Add(exact);
            }
        }
    }

    private void AddCustomDefault(ReservedEntry entry, bool includeInPartialMatching = true)
    {
        Add(entry, includeInPartialMatching);

        var exact = NormalizeExact(entry.Value);
        if (exact is null)
        {
            return;
        }

        if (!_customExact.ContainsKey(exact))
        {
            _customExact.Add(exact, entry);
        }

        var compact = NormalizeCompact(exact);
        if (compact.Length > 0 && !_customCompact.ContainsKey(compact))
        {
            _customCompact.Add(compact, entry);
        }

        if (includeInPartialMatching && compact.Length >= _partialMatchMinimumLength)
        {
            _customPartialEntries.Add(new PartialEntry(exact, compact, entry));
        }
    }

    private void AddExactCustom(ReservedEntry entry)
    {
        var exact = NormalizeExact(entry.Value);
        if (exact is not null && !_exactOnlyCustom.ContainsKey(exact))
        {
            _exactOnlyCustom.Add(exact, entry);
        }
    }

    private static void AddGeographyRuleValue(
        string value,
        Dictionary<string, string> exactValues,
        Dictionary<string, string> compactValues)
    {
        var exact = NormalizeExact(value);
        if (exact is null)
        {
            return;
        }

        if (!exactValues.ContainsKey(exact))
        {
            exactValues.Add(exact, value);
        }

        var compact = NormalizeCompact(exact);
        if (compact.Length > 0 && !compactValues.ContainsKey(compact))
        {
            compactValues.Add(compact, value);
        }
    }

    private void AddIdentityRuleValue(string value, string category)
    {
        var exact = NormalizeExact(value);
        if (exact is null)
        {
            return;
        }

        var entry = new ReservedEntry(value, category);
        if (!_identityRuleExact.ContainsKey(exact))
        {
            _identityRuleExact.Add(exact, entry);
        }

        var compact = NormalizeCompact(exact);
        if (compact.Length > 0 && !_identityRuleCompact.ContainsKey(compact))
        {
            _identityRuleCompact.Add(compact, entry);
        }
    }

    private Result? CheckExactCustomReservation(string? value, string exact)
    {
        var optionalRuleResult = CheckOptionalRuleReservation(value, exact);
        if (optionalRuleResult is not null)
        {
            return optionalRuleResult;
        }

        if (!_exactOnlyCustom.TryGetValue(exact, out var match))
        {
            return null;
        }

        MapOriginalSpanFromInput(

            value,

            exact,

            useCompactMapping: false,

            0,

            exact.Length,

            out var originalStart,

            out var originalLength);
        return CreateReservedResult(
            value,
            match,
            MatchKind.Exact,
            0,
            exact.Length,
            originalStart,
            originalLength);
    }

    private Result? CheckOptionalRuleReservation(string? value, string exact)
    {
        if (!IsRuleException(Rule.CountryNames, value)
            && _countryRuleExact.TryGetValue(exact, out var country))
        {
            return new Result(true, value, country, null, MatchKind.CountryName);
        }

        if (!IsRuleException(Rule.PopularCityNames, value)
            && _cityRuleExact.TryGetValue(exact, out var city))
        {
            return new Result(true, value, city, null, MatchKind.PopularCityName);
        }

        if (_identityRuleExact.TryGetValue(exact, out var identityExact))
        {
            var identityRule = GetRuleForIdentityCategory(identityExact.Category);
            if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
            {
                MapOriginalSpanFromInput(
                    value,
                    exact,
                    useCompactMapping: false,
                    0,
                    exact.Length,
                    out var originalStart,
                    out var originalLength);
                return CreateReservedResult(
                    value,
                    identityExact,
                    MatchKind.Exact,
                    0,
                    exact.Length,
                    originalStart,
                    originalLength);
            }
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching
            && !IsRuleException(Rule.CompactMatching, value)
            && compact.Length > 0)
        {
            if (!IsRuleException(Rule.CountryNames, value)
                && _countryRuleCompact.TryGetValue(compact, out country))
            {
                return new Result(true, value, country, null, MatchKind.CountryName);
            }

            if (!IsRuleException(Rule.PopularCityNames, value)
                && _cityRuleCompact.TryGetValue(compact, out city))
            {
                return new Result(true, value, city, null, MatchKind.PopularCityName);
            }

            if (_identityRuleCompact.TryGetValue(compact, out var identityCompact))
            {
                var identityRule = GetRuleForIdentityCategory(identityCompact.Category);
                if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
                {
                    MapOriginalSpanFromInput(
                        value,
                        exact,
                        useCompactMapping: true,
                        0,
                        compact.Length,
                        out var originalStart,
                        out var originalLength);
                    return CreateReservedResult(
                        value,
                        identityCompact,
                        MatchKind.Compact,
                        0,
                        compact.Length,
                        originalStart,
                        originalLength);
                }
            }
        }

        var obfuscationInput = NormalizeCaseAwareObfuscationInput(value, exact);

        if (_unicodeConfusableMatching
            && !IsRuleException(Rule.UnicodeConfusableMatching, value)
            && TryMatchUnicodeConfusable(
                obfuscationInput,
                _identityRuleExact,
                _identityRuleCompact,
                NoPartialEntries,
                out var confusableMatch,
                out var confusableKind,
                out var confusableStart,
                out var confusableLength))
        {
            var identityRule = GetRuleForIdentityCategory(confusableMatch!.Category);
            if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
            {
                return CreateReservedResult(
                    value,
                    confusableMatch,
                    confusableKind,
                    confusableStart,
                    confusableLength);
            }
        }

        if (_obfuscationMatching
            && !IsRuleException(Rule.ObfuscationMatching, value)
            && TryMatchObfuscated(
                obfuscationInput,
                _identityRuleExact,
                _identityRuleCompact,
                NoPartialEntries,
                out var obfuscatedMatch,
                out var obfuscatedKind,
                out var obfuscatedStart,
                out var obfuscatedLength))
        {
            var identityRule = GetRuleForIdentityCategory(obfuscatedMatch!.Category);
            if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
            {
                return CreateReservedResult(
                    value,
                    obfuscatedMatch,
                    obfuscatedKind,
                    obfuscatedStart,
                    obfuscatedLength);
            }
        }

        return null;
    }

    private Result? CheckCustomDefaultReservations(string? value, string exact)
    {
        if (_customExact.TryGetValue(exact, out var exactMatch))
        {
            MapOriginalSpanFromInput(
                value,
                exact,
                useCompactMapping: false,
                0,
                exact.Length,
                out var originalStart,
                out var originalLength);
            return CreateReservedResult(
                value,
                exactMatch,
                MatchKind.Exact,
                0,
                exact.Length,
                originalStart,
                originalLength);
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching && compact.Length > 0 && _customCompact.TryGetValue(compact, out var compactMatch))
        {
            MapOriginalSpanFromInput(
                value,
                exact,
                useCompactMapping: true,
                0,
                compact.Length,
                out var originalStart,
                out var originalLength);
            return CreateReservedResult(
                value,
                compactMatch,
                MatchKind.Compact,
                0,
                compact.Length,
                originalStart,
                originalLength);
        }

        if (_partialMatching
            && TryMatchPartial(
                exact,
                compact,
                _customPartialEntries,
                out var partialMatch,
                out var partialStart,
                out var partialLength,
                out var usedCompact))
        {
            MapOriginalSpanFromInput(
                value,
                exact,
                useCompactMapping: usedCompact,
                partialStart,
                partialLength,
                out var originalStart,
                out var originalLength);
            return CreateReservedResult(
                value,
                partialMatch!,
                MatchKind.Partial,
                partialStart,
                partialLength,
                originalStart,
                originalLength);
        }

        var obfuscationInput = NormalizeCaseAwareObfuscationInput(value, exact);

        if (_unicodeConfusableMatching
            && TryMatchUnicodeConfusable(
                obfuscationInput,
                _customExact,
                _customCompact,
                _customPartialEntries,
                out var confusableMatch,
                out var confusableKind,
                out var confusableStart,
                out var confusableLength))
        {
            return CreateReservedResult(value, confusableMatch!, confusableKind, confusableStart, confusableLength);
        }

        if (_obfuscationMatching
            && TryMatchObfuscated(
                obfuscationInput,
                _customExact,
                _customCompact,
                _customPartialEntries,
                out var obfuscatedMatch,
                out var obfuscatedKind,
                out var obfuscatedStart,
                out var obfuscatedLength))
        {
            return CreateReservedResult(value, obfuscatedMatch!, obfuscatedKind, obfuscatedStart, obfuscatedLength);
        }

        return null;
    }
}
