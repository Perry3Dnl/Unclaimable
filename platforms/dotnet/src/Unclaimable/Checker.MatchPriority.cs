namespace Unclaimable;

public sealed partial class Checker
{
    private bool TryMatchZeroCostObfuscation(
        string value,
        ObfuscationIndex index,
        bool preserveNonCompactCharacters,
        HashSet<long> failedStates,
        out ReservedEntry? match,
        out MatchKind kind,
        out int? start,
        out int? length)
    {
        var entries = preserveNonCompactCharacters
            ? index.ExactEntries
            : index.CompactEntries;

        foreach (var pair in entries)
        {
            if (!CanObfuscationLengthsMatch(
                    value, pair.Key.Length, preserveNonCompactCharacters, 0, directPotential: true))
            {
                continue;
            }

            int end;
            if (!TryMatchObfuscationText(
                    value, 0, pair.Key, preserveNonCompactCharacters, 0, true, failedStates, out end))
            {
                continue;
            }

            match = pair.Value;
            kind = MatchKind.Obfuscated;
            start = 0;
            length = end;
            return true;
        }

        if (_partialMatching)
        {
            for (var entryIndex = 0; entryIndex < index.PartialEntries.Count; entryIndex++)
            {
                var partial = index.PartialEntries[entryIndex];
                var target = preserveNonCompactCharacters ? partial.Exact : partial.Compact;
                if (value.Length <= target.Length)
                {
                    continue;
                }

                for (var sourceStart = 0; sourceStart < value.Length; sourceStart++)
                {
                    int end;
                    if (!TryMatchObfuscationText(
                            value, sourceStart, target,
                            preserveNonCompactCharacters, 0, false, failedStates, out end))
                    {
                        continue;
                    }

                    match = partial.Entry;
                    kind = MatchKind.Partial;
                    start = sourceStart;
                    length = Math.Max(1, end - sourceStart);
                    return true;
                }
            }
        }

        match = null;
        kind = MatchKind.None;
        start = null;
        length = null;
        return false;
    }
}
