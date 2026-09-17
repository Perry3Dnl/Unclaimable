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

    private void AddCustomDefault(ReservedEntry entry)
    {
        Add(entry);

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

        if (compact.Length >= _partialMatchMinimumLength)
        {
            _customPartialEntries.Add(new PartialEntry(exact, compact, entry));
        }
    }

    private void AddExactCustom(ReservedEntry entry)
    {
        if (TryAddOptionalRuleReservation(entry.Value))
        {
            return;
        }

        var exact = NormalizeExact(entry.Value);
        if (exact is not null && !_exactOnlyCustom.ContainsKey(exact))
        {
            _exactOnlyCustom.Add(exact, entry);
        }
    }

    private bool TryAddOptionalRuleReservation(string value)
    {
        if (value.StartsWith(GeographyData.CountryReservationPrefix, StringComparison.Ordinal))
        {
            AddGeographyRuleValue(
                value.Substring(GeographyData.CountryReservationPrefix.Length),
                _countryRuleExact,
                _countryRuleCompact);
            return true;
        }

        if (value.StartsWith(GeographyData.CityReservationPrefix, StringComparison.Ordinal))
        {
            AddGeographyRuleValue(
                value.Substring(GeographyData.CityReservationPrefix.Length),
                _cityRuleExact,
                _cityRuleCompact);
            return true;
        }

        if (TryAddIdentityRuleReservation(value, CelebrityData.ReservationPrefix, "celebrity")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.NationalityPrefix, "nationality")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.CurrencyPrefix, "currency")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.ReligionPrefix, "religion")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.LandmarkPrefix, "landmark")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.EventPrefix, "event")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.AwardPrefix, "award")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.FictionalCharacterPrefix, "fictionalcharacter")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.FranchisePrefix, "franchise")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.ProfessionPrefix, "profession")
            || TryAddIdentityRuleReservation(value, OptionalIdentityData.MilitaryPrefix, "military"))
        {
            return true;
        }

        return false;
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

    private bool TryAddIdentityRuleReservation(string reservation, string prefix, string category)
    {
        if (!reservation.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        AddIdentityRuleValue(reservation.Substring(prefix.Length), category);
        return true;
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

        var mapping = value is null ? null : TryCreateInputMapping(value, exact);
        TryMapOriginalSpan(mapping?.ExactToOriginal, 0, exact.Length, out var originalStart, out var originalLength);
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
        if (_countryRuleExact.TryGetValue(exact, out var country))
        {
            return new Result(true, value, country, null, MatchKind.CountryName);
        }

        if (_cityRuleExact.TryGetValue(exact, out var city))
        {
            return new Result(true, value, city, null, MatchKind.PopularCityName);
        }

        if (_identityRuleExact.TryGetValue(exact, out var identityExact))
        {
            var mapping = value is null ? null : TryCreateInputMapping(value, exact);
            TryMapOriginalSpan(mapping?.ExactToOriginal, 0, exact.Length, out var originalStart, out var originalLength);
            return CreateReservedResult(
                value,
                identityExact,
                MatchKind.Exact,
                0,
                exact.Length,
                originalStart,
                originalLength);
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching && compact.Length > 0)
        {
            if (_countryRuleCompact.TryGetValue(compact, out country))
            {
                return new Result(true, value, country, null, MatchKind.CountryName);
            }

            if (_cityRuleCompact.TryGetValue(compact, out city))
            {
                return new Result(true, value, city, null, MatchKind.PopularCityName);
            }

            if (_identityRuleCompact.TryGetValue(compact, out var identityCompact))
            {
                var mapping = value is null ? null : TryCreateInputMapping(value, exact);
                TryMapOriginalSpan(mapping?.CompactToOriginal, 0, compact.Length, out var originalStart, out var originalLength);
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

        if (_unicodeConfusableMatching
            && TryMatchUnicodeConfusable(
                exact,
                _identityRuleExact,
                _identityRuleCompact,
                NoPartialEntries,
                out var confusableMatch,
                out var confusableKind,
                out var confusableStart,
                out var confusableLength))
        {
            return CreateReservedResult(
                value,
                confusableMatch!,
                confusableKind,
                confusableStart,
                confusableLength);
        }

        if (_obfuscationMatching
            && TryMatchObfuscated(
                exact,
                _identityRuleExact,
                _identityRuleCompact,
                NoPartialEntries,
                out var obfuscatedMatch,
                out var obfuscatedKind,
                out var obfuscatedStart,
                out var obfuscatedLength))
        {
            return CreateReservedResult(
                value,
                obfuscatedMatch!,
                obfuscatedKind,
                obfuscatedStart,
                obfuscatedLength);
        }

        return null;
    }

    private Result? CheckCustomDefaultReservations(string? value, string exact)
    {
        if (_customExact.TryGetValue(exact, out var exactMatch))
        {
            var mapping = value is null ? null : TryCreateInputMapping(value, exact);
            TryMapOriginalSpan(mapping?.ExactToOriginal, 0, exact.Length, out var originalStart, out var originalLength);
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
            var mapping = value is null ? null : TryCreateInputMapping(value, exact);
            TryMapOriginalSpan(mapping?.CompactToOriginal, 0, compact.Length, out var originalStart, out var originalLength);
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
            var mapping = value is null ? null : TryCreateInputMapping(value, exact);
            var sourceMap = usedCompact ? mapping?.CompactToOriginal : mapping?.ExactToOriginal;
            TryMapOriginalSpan(sourceMap, partialStart, partialLength, out var originalStart, out var originalLength);
            return CreateReservedResult(
                value,
                partialMatch!,
                MatchKind.Partial,
                partialStart,
                partialLength,
                originalStart,
                originalLength);
        }

        if (_unicodeConfusableMatching
            && TryMatchUnicodeConfusable(
                exact,
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
                exact,
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
