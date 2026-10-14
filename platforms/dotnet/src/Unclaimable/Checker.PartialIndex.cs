using System;
using System.Collections.Generic;

namespace Unclaimable;

public sealed partial class Checker
{
    // Immutable after construction. Only entries whose initial UTF-16 characters
    // occur in the candidate input are considered for partial matching.
    private sealed class PartialCandidateIndex
    {
        private const ulong SingleCharacterFlag = 1UL << 48;
        private const ulong TwoCharacterFlag = 2UL << 48;
        private const ulong ThreeCharacterFlag = 3UL << 48;
        private readonly Dictionary<ulong, int[]> _exact;
        private readonly Dictionary<ulong, int[]> _compact;

        public PartialCandidateIndex(IReadOnlyList<PartialEntry> entries)
        {
            _exact = Build(entries, compact: false);
            _compact = Build(entries, compact: true);
        }

        private static Dictionary<ulong, int[]> Build(IReadOnlyList<PartialEntry> entries, bool compact)
        {
            var pending = new Dictionary<ulong, List<int>>();
            for (var i = 0; i < entries.Count; i++)
            {
                var pattern = compact ? entries[i].Compact : entries[i].Exact;
                if (pattern.Length == 0)
                {
                    continue;
                }

                var key = Prefix(pattern, 0);
                if (!pending.TryGetValue(key, out var indexes))
                {
                    indexes = new List<int>();
                    pending.Add(key, indexes);
                }

                indexes.Add(i);
            }

            var result = new Dictionary<ulong, int[]>(pending.Count);
            foreach (var pair in pending)
            {
                result.Add(pair.Key, pair.Value.ToArray());
            }

            return result;
        }

        private static ulong Prefix(string text, int start)
        {
            var remaining = text.Length - start;
            if (remaining >= 3)
            {
                return ThreeCharacterFlag | ((ulong)text[start] << 32)
                    | ((ulong)text[start + 1] << 16) | text[start + 2];
            }

            return remaining == 2
                ? TwoCharacterFlag | ((ulong)text[start] << 16) | text[start + 1]
                : SingleCharacterFlag | text[start];
        }

        public bool TryMatch(
            Checker checker,
            string exact,
            string compact,
            IReadOnlyList<PartialEntry> entries,
            bool compactEnabled,
            Rule ruleExceptions,
            out ReservedEntry? match,
            out int startIndex,
            out int matchLength,
            out bool usedCompact)
        {
            var bestIndex = int.MaxValue;
            var bestStart = -1;
            var bestCompact = false;

            Scan(checker, exact, entries, _exact, false, ruleExceptions,
                ref bestIndex, ref bestStart, ref bestCompact);

            if (compactEnabled)
            {
                Scan(checker, compact, entries, _compact, true, ruleExceptions,
                    ref bestIndex, ref bestStart, ref bestCompact);
            }

            if (bestIndex != int.MaxValue)
            {
                var selected = entries[bestIndex];
                match = selected.Entry;
                startIndex = bestStart;
                matchLength = bestCompact ? selected.Compact.Length : selected.Exact.Length;
                usedCompact = bestCompact;
                return true;
            }

            match = null;
            startIndex = -1;
            matchLength = 0;
            usedCompact = false;
            return false;
        }

        private static void Scan(
            Checker checker,
            string input,
            IReadOnlyList<PartialEntry> entries,
            Dictionary<ulong, int[]> buckets,
            bool compact,
            Rule ruleExceptions,
            ref int bestIndex,
            ref int bestStart,
            ref bool bestCompact)
        {
            for (var start = 0; start < input.Length; start++)
            {
                if (start + 2 < input.Length)
                {
                    ScanBucket(ThreeCharacterFlag | ((ulong)input[start] << 32)
                        | ((ulong)input[start + 1] << 16) | input[start + 2],
                        checker, input, entries, buckets, compact, ruleExceptions,
                        start, ref bestIndex, ref bestStart, ref bestCompact);
                }

                if (start + 1 < input.Length)
                {
                    ScanBucket(TwoCharacterFlag | ((ulong)input[start] << 16) | input[start + 1],
                        checker, input, entries, buckets, compact, ruleExceptions,
                        start, ref bestIndex, ref bestStart, ref bestCompact);
                }

                ScanBucket(SingleCharacterFlag | input[start], checker, input, entries, buckets,
                    compact, ruleExceptions, start, ref bestIndex, ref bestStart, ref bestCompact);
            }
        }

        private static void ScanBucket(
            ulong key,
            Checker checker,
            string input,
            IReadOnlyList<PartialEntry> entries,
            Dictionary<ulong, int[]> buckets,
            bool compact,
            Rule ruleExceptions,
            int start,
            ref int bestIndex,
            ref int bestStart,
            ref bool bestCompact)
        {
            if (!buckets.TryGetValue(key, out var candidates))
            {
                return;
            }

            foreach (var index in candidates)
            {
                // Exact scans run first, so a compact hit cannot replace the same
                // entry's exact hit. Earlier entry order always takes priority.
                if (index > bestIndex || (index == bestIndex && (bestCompact == false || !compact)))
                {
                    continue;
                }

                var entry = entries[index];
                var pattern = compact ? entry.Compact : entry.Exact;
                if (input.Length <= pattern.Length || start + pattern.Length > input.Length)
                {
                    continue;
                }

                if (ruleExceptions != Rule.None)
                {
                    var sourceRule = GetRuleForReservedEntry(entry.Entry);
                    if (sourceRule.HasValue && HasRuleException(ruleExceptions, sourceRule.Value))
                    {
                        continue;
                    }
                }

                if (string.CompareOrdinal(input, start, pattern, 0, pattern.Length) != 0
                    || checker.ShouldSuppressSlangPartial(entry.Entry, input.Length, start, pattern.Length))
                {
                    continue;
                }

                if (index < bestIndex || (index == bestIndex && start < bestStart))
                {
                    bestIndex = index;
                    bestStart = start;
                    bestCompact = compact;
                }
            }
        }
    }

    private readonly PartialCandidateIndex? _mainPartialCandidateIndex;
    private readonly PartialCandidateIndex? _customPartialCandidateIndex;
}
