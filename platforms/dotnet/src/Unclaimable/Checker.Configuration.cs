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
