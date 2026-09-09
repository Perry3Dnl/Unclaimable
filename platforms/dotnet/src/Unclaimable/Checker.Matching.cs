using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private void Add(ReservedEntry entry, bool includeInPartialMatching = true)
    {
        var exact = NormalizeExact(entry.Value);
        if (exact is null)
        {
            return;
        }

        if (!_exact.ContainsKey(exact))
        {
            _exact.Add(exact, entry);
        }

        var compact = NormalizeCompact(exact);
        if (compact.Length > 0 && !_compact.ContainsKey(compact))
        {
            _compact.Add(compact, entry);
        }

        if (includeInPartialMatching && compact.Length >= _partialMatchMinimumLength)
        {
            _partialEntries.Add(new PartialEntry(exact, compact, entry));
        }
    }

    private bool TryMatchPartial(
        string exact,
        string compact,
        out ReservedEntry? match,
        out int startIndex,
        out int matchLength,
        out bool usedCompact)
    {
        foreach (var partial in _partialEntries)
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

    private bool TryMatchUnicodeConfusable(
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

        if (_exact.TryGetValue(skeleton, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = skeleton.Length;
            return true;
        }

        var compact = NormalizeCompact(skeleton);
        if (_compactMatching && compact.Length > 0 && _compact.TryGetValue(compact, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = compact.Length;
            return true;
        }

        if (_partialMatching)
        {
            int partialStart;
            int partialLength;
            bool usedCompact;
            if (TryMatchPartial(skeleton, compact, out match, out partialStart, out partialLength, out usedCompact))
            {
                matchKind = MatchKind.Partial;
                matchStartIndex = partialStart;
                matchLength = partialLength;
                return true;
            }
        }

        if (_obfuscationMatching
            && TryMatchObfuscated(skeleton, out match, out matchKind, out matchStartIndex, out matchLength))
        {
            return true;
        }

        match = null;
        matchKind = MatchKind.None;
        matchStartIndex = null;
        matchLength = null;
        return false;
    }

    private bool TryMatchObfuscated(
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
                var directEntries = preserveNonCompactCharacters ? _exact : _compact;
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
                foreach (var partial in _partialEntries)
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
