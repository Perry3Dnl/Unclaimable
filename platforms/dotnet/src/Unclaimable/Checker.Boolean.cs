namespace Unclaimable;

public sealed partial class Checker
{
    private bool IsReservedCore(string value)
    {
        if (TryFindFirstPolicyViolation(value, out _))
        {
            return true;
        }

        if (TryFindFirstPatternViolation(value, out _))
        {
            return true;
        }

        return IsReservedNameFast(value);
    }

    private bool IsReservedNameFast(string value)
    {
        var exact = NormalizeExact(value);
        if (exact is null)
        {
            return false;
        }

        var ruleExceptions = GetRuleExceptions(value);

        if (IsExactCustomReservationFast(value, exact))
        {
            return true;
        }

        if (_allowedIdentifiers.Contains(exact))
        {
            return IsCustomDefaultReservationFast(value, exact);
        }

        if (_exact.TryGetValue(exact, out var exactMatch))
        {
            var sourceRule = GetRuleForReservedEntry(exactMatch);
            if (!sourceRule.HasValue || !HasRuleException(ruleExceptions, sourceRule.Value))
            {
                return true;
            }
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching
            && !HasRuleException(ruleExceptions, Rule.CompactMatching)
            && compact.Length > 0
            && _compact.TryGetValue(compact, out var compactMatch))
        {
            var sourceRule = GetRuleForReservedEntry(compactMatch);
            if (!sourceRule.HasValue || !HasRuleException(ruleExceptions, sourceRule.Value))
            {
                return true;
            }
        }

        if (_partialMatching
            && !HasRuleException(ruleExceptions, Rule.PartialMatching)
            && TryMatchPartial(
                exact,
                compact,
                out _,
                out _,
                out _,
                out _,
                ruleExceptions))
        {
            return true;
        }

        var checkConfusables =
            _unicodeConfusableMatching
            && !HasRuleException(ruleExceptions, Rule.UnicodeConfusableMatching);
        var checkObfuscation =
            _obfuscationMatching
            && !HasRuleException(ruleExceptions, Rule.ObfuscationMatching);

        if (!checkConfusables && !checkObfuscation)
        {
            return false;
        }

        var obfuscationInput = NormalizeCaseAwareObfuscationInput(value, exact);

        if (checkConfusables
            && TryMatchUnicodeConfusable(
                obfuscationInput,
                out var confusableMatch,
                out _,
                out _,
                out _))
        {
            var sourceRule = GetRuleForReservedEntry(confusableMatch!);
            if (!sourceRule.HasValue || !HasRuleException(ruleExceptions, sourceRule.Value))
            {
                return true;
            }
        }

        if (checkObfuscation
            && TryMatchObfuscated(
                obfuscationInput,
                out var obfuscatedMatch,
                out _,
                out _,
                out _))
        {
            var sourceRule = GetRuleForReservedEntry(obfuscatedMatch!);
            if (!sourceRule.HasValue || !HasRuleException(ruleExceptions, sourceRule.Value))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsExactCustomReservationFast(string value, string exact)
    {
        return IsOptionalRuleReservationFast(value, exact)
               || _exactOnlyCustom.ContainsKey(exact);
    }

    private bool IsOptionalRuleReservationFast(string value, string exact)
    {
        if (!IsRuleException(Rule.CountryNames, value)
            && _countryRuleExact.ContainsKey(exact))
        {
            return true;
        }

        if (!IsRuleException(Rule.PopularCityNames, value)
            && _cityRuleExact.ContainsKey(exact))
        {
            return true;
        }

        if (_identityRuleExact.TryGetValue(exact, out var identityExact))
        {
            var identityRule = GetRuleForIdentityCategory(identityExact.Category);
            if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
            {
                return true;
            }
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching
            && !IsRuleException(Rule.CompactMatching, value)
            && compact.Length > 0)
        {
            if (!IsRuleException(Rule.CountryNames, value)
                && _countryRuleCompact.ContainsKey(compact))
            {
                return true;
            }

            if (!IsRuleException(Rule.PopularCityNames, value)
                && _cityRuleCompact.ContainsKey(compact))
            {
                return true;
            }

            if (_identityRuleCompact.TryGetValue(compact, out var identityCompact))
            {
                var identityRule = GetRuleForIdentityCategory(identityCompact.Category);
                if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
                {
                    return true;
                }
            }
        }

        var checkConfusables =
            _unicodeConfusableMatching
            && !IsRuleException(Rule.UnicodeConfusableMatching, value);
        var checkObfuscation =
            _obfuscationMatching
            && !IsRuleException(Rule.ObfuscationMatching, value);

        if (!checkConfusables && !checkObfuscation)
        {
            return false;
        }

        var obfuscationInput = NormalizeCaseAwareObfuscationInput(value, exact);

        if (checkConfusables
            && TryMatchUnicodeConfusable(
                obfuscationInput,
                _identityRuleExact,
                _identityRuleCompact,
                NoPartialEntries,
                out var confusableMatch,
                out _,
                out _,
                out _))
        {
            var identityRule = GetRuleForIdentityCategory(confusableMatch!.Category);
            if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
            {
                return true;
            }
        }

        if (checkObfuscation
            && TryMatchObfuscated(
                obfuscationInput,
                _identityRuleExact,
                _identityRuleCompact,
                NoPartialEntries,
                out var obfuscatedMatch,
                out _,
                out _,
                out _))
        {
            var identityRule = GetRuleForIdentityCategory(obfuscatedMatch!.Category);
            if (!identityRule.HasValue || !IsRuleException(identityRule.Value, value))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCustomDefaultReservationFast(string value, string exact)
    {
        if (_customExact.ContainsKey(exact))
        {
            return true;
        }

        var compact = NormalizeCompact(exact);
        if (_compactMatching
            && compact.Length > 0
            && _customCompact.ContainsKey(compact))
        {
            return true;
        }

        if (_partialMatching
            && TryMatchPartial(
                exact,
                compact,
                _customPartialEntries,
                out _,
                out _,
                out _,
                out _))
        {
            return true;
        }

        if (!_unicodeConfusableMatching && !_obfuscationMatching)
        {
            return false;
        }

        var obfuscationInput = NormalizeCaseAwareObfuscationInput(value, exact);

        if (_unicodeConfusableMatching
            && TryMatchUnicodeConfusable(
                obfuscationInput,
                _customExact,
                _customCompact,
                _customPartialEntries,
                out _,
                out _,
                out _,
                out _))
        {
            return true;
        }

        return _obfuscationMatching
               && TryMatchObfuscated(
                   obfuscationInput,
                   _customExact,
                   _customCompact,
                   _customPartialEntries,
                   out _,
                   out _,
                   out _,
                   out _);
    }
}
