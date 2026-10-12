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
        out bool usedCompact) =>
        TryMatchPartial(exact, compact, _partialEntries, out match, out startIndex, out matchLength, out usedCompact);

    private bool TryMatchPartial(
        string exact,
        string compact,
        IReadOnlyList<PartialEntry> partialEntries,
        out ReservedEntry? match,
        out int startIndex,
        out int matchLength,
        out bool usedCompact)
    {
        foreach (var partial in partialEntries)
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
        out int? matchLength) =>
        TryMatchUnicodeConfusable(
            value,
            _exact,
            _compact,
            _partialEntries,
            out match,
            out matchKind,
            out matchStartIndex,
            out matchLength);

    private bool TryMatchUnicodeConfusable(
        string value,
        IReadOnlyDictionary<string, ReservedEntry> exactEntries,
        IReadOnlyDictionary<string, ReservedEntry> compactEntries,
        IReadOnlyList<PartialEntry> partialEntries,
        out ReservedEntry? match,
        out MatchKind matchKind,
        out int? matchStartIndex,
        out int? matchLength)
    {
        bool changed;
        var skeleton = ConfusableNormalizer.CreateSkeleton(value, includeAsciiObfuscation: false, out changed);
        if (!changed)
        {
            match = null;
            matchKind = MatchKind.None;
            matchStartIndex = null;
            matchLength = null;
            return false;
        }

        if (exactEntries.TryGetValue(skeleton, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = skeleton.Length;
            return true;
        }

        var compact = NormalizeCompact(skeleton);
        if (_compactMatching && compact.Length > 0 && compactEntries.TryGetValue(compact, out match))
        {
            matchKind = MatchKind.UnicodeConfusable;
            matchStartIndex = 0;
            matchLength = compact.Length;
            return true;
        }

        if (_partialMatching
            && TryMatchPartial(
                skeleton,
                compact,
                partialEntries,
                out match,
                out var partialStart,
                out var partialLength,
                out _))
        {
            matchKind = MatchKind.Partial;
            matchStartIndex = partialStart;
            matchLength = partialLength;
            return true;
        }

        if (_obfuscationMatching
            && TryMatchObfuscated(
                skeleton,
                exactEntries,
                compactEntries,
                partialEntries,
                out match,
                out matchKind,
                out matchStartIndex,
                out matchLength))
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
        out int? matchLength) =>
        TryMatchObfuscated(
            value,
            _exact,
            _compact,
            _partialEntries,
            out match,
            out matchKind,
            out matchStartIndex,
            out matchLength);

    private bool TryMatchObfuscated(
        string value,
        IReadOnlyDictionary<string, ReservedEntry> exactEntries,
        IReadOnlyDictionary<string, ReservedEntry> compactEntries,
        IReadOnlyList<PartialEntry> partialEntries,
        out ReservedEntry? match,
        out MatchKind matchKind,
        out int? matchStartIndex,
        out int? matchLength)
    {
        if (_obfuscationSensitivity <= ObfuscationSensitivity.Medium
            && !HasDirectObfuscationPotential(value))
        {
            match = null;
            matchKind = MatchKind.None;
            matchStartIndex = null;
            matchLength = null;
            return false;
        }

        var index = ResolveObfuscationIndex(exactEntries);
        var preserveNonCompactCharacters = _consistentCompactMatching && !_compactMatching;

        return TryMatchObfuscationIndex(
            value,
            index,
            preserveNonCompactCharacters,
            out match,
            out matchKind,
            out matchStartIndex,
            out matchLength);
    }
}
