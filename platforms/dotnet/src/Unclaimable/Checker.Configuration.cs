using System.Globalization;

namespace Unclaimable;

public sealed partial class Checker
{
    private readonly HashSet<string> _allowedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _exactOnlyCustom = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _customExact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly Dictionary<string, ReservedEntry> _customCompact = new Dictionary<string, ReservedEntry>(StringComparer.Ordinal);
    private readonly List<PartialEntry> _customPartialEntries = new List<PartialEntry>();

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
        var exact = NormalizeExact(entry.Value);
        if (exact is not null && !_exactOnlyCustom.ContainsKey(exact))
        {
            _exactOnlyCustom.Add(exact, entry);
        }
    }

    private Result? CheckExactCustomReservation(string? value, string exact)
    {
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
            && TryMatchCustomPartial(exact, compact, out var partialMatch, out var partialStart, out var partialLength, out var usedCompact))
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
            && TryMatchCustomUnicodeConfusable(exact, out var confusableMatch, out var confusableKind, out var confusableStart, out var confusableLength))
        {
            return CreateReservedResult(value, confusableMatch!, confusableKind, confusableStart, confusableLength);
        }

        if (_obfuscationMatching
            && TryMatchCustomObfuscated(exact, out var obfuscatedMatch, out var obfuscatedKind, out var obfuscatedStart, out var obfuscatedLength))
        {
            return CreateReservedResult(value, obfuscatedMatch!, obfuscatedKind, obfuscatedStart, obfuscatedLength);
        }

        return null;
    }

    private bool TryMatchCustomPartial(
        string exact,
        string compact,
        out ReservedEntry? match,
        out int startIndex,
        out int matchLength,
        out bool usedCompact)
    {
        foreach (var partial in _customPartialEntries)
        {
            var exactIndex = exact.IndexOf(partial.Exact, StringComparison.Ordinal);
            if (exactIndex >= 0 && exact.Length > partial.Exact.Length)
            {
                match = partial.Entry;
                startIndex = exactIndex;
                matchLength = partial.Exact.Length;
                usedCompact = false;
                return true;
            }

            var compactPartialEnabled = !_consistentCompactMatching || _compactMatching;
            if (compactPartialEnabled && compact.Length > partial.Compact.Length)
            {
                var compactIndex = compact.IndexOf(partial.Compact, StringComparison.Ordinal);
                if (compactIndex >= 0)
                {
                    match = partial.Entry;
                    startIndex = compactIndex;
                    matchLength = partial.Compact.Length;
                    usedCompact = true;
                    return true;
                }
            }
        }

        match = null;
        startIndex = -1;
        matchLength = 0;
        usedCompact = false;
        return false;
    }

    private bool TryMatchCustomUnicodeConfusable(
        string value,
        out ReservedEntry? match,
        out MatchKind matchKind,
        out int? matchStartIndex,
        out int? matchLength)
    {
        bool changed;
        var skeleton = NormalizeUnicodeConfusables(value, out changed);
        if (!changed)
        {
            match = null;
            matchKind = MatchKind.None;
            matchStartIndex = null;
            matchLength = null;
            return false;
        }

        if (_customExact.TryGetValue(skeleton, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = skeleton.Length;
            return true;
        }

        var compact = NormalizeCompact(skeleton);
        if (_compactMatching && compact.Length > 0 && _customCompact.TryGetValue(compact, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = compact.Length;
            return true;
        }

        if (_partialMatching)
        {
            if (TryMatchCustomPartial(skeleton, compact, out match, out var partialStart, out var partialLength, out _))
            {
                matchKind = MatchKind.Partial;
                matchStartIndex = partialStart;
                matchLength = partialLength;
                return true;
            }
        }

        if (_obfuscationMatching
            && TryMatchCustomObfuscated(skeleton, out match, out matchKind, out matchStartIndex, out matchLength))
        {
            return true;
        }

        match = null;
        matchKind = MatchKind.None;
        matchStartIndex = null;
        matchLength = null;
        return false;
    }

    private bool TryMatchCustomObfuscated(
        string value,
        out ReservedEntry? match,
        out MatchKind matchKind,
        out int? matchStartIndex,
        out int? matchLength)
    {
        var candidates = new List<string> { string.Empty };
        var usedSubstitution = false;
        var preserveNonCompactCharacters = _consistentCompactMatching && !_compactMatching;

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            string[]? substitutions;

            if (TryGetObfuscationSubstitutions(character, out substitutions))
            {
                usedSubstitution = true;
                candidates = ExpandCandidates(candidates, substitutions!);
                continue;
            }

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                var scalarText = new string(new[] { character, value[index + 1] });
                var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
                if (IsLetterOrDigit(category) || preserveNonCompactCharacters)
                {
                    AppendToCandidates(candidates, scalarText);
                }

                index++;
                continue;
            }

            if (char.IsLetterOrDigit(character) || preserveNonCompactCharacters)
            {
                AppendToCandidates(candidates, character.ToString());
            }
        }

        if (!usedSubstitution)
        {
            match = null;
            matchKind = MatchKind.None;
            matchStartIndex = null;
            matchLength = null;
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Length > 0)
            {
                var directEntries = preserveNonCompactCharacters ? _customExact : _customCompact;
                if (directEntries.TryGetValue(candidate, out match))
                {
                    matchKind = MatchKind.Obfuscated;
                    matchStartIndex = 0;
                    matchLength = candidate.Length;
                    return true;
                }
            }

            if (_partialMatching)
            {
                foreach (var partial in _customPartialEntries)
                {
                    var partialValue = preserveNonCompactCharacters ? partial.Exact : partial.Compact;
                    if (candidate.Length <= partialValue.Length)
                    {
                        continue;
                    }

                    var partialIndex = candidate.IndexOf(partialValue, StringComparison.Ordinal);
                    if (partialIndex >= 0)
                    {
                        match = partial.Entry;
                        matchKind = MatchKind.Partial;
                        matchStartIndex = partialIndex;
                        matchLength = partialValue.Length;
                        return true;
                    }
                }
            }
        }

        match = null;
        matchKind = MatchKind.None;
        matchStartIndex = null;
        matchLength = null;
        return false;
    }
}
