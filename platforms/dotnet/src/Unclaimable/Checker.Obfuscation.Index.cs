namespace Unclaimable;

public sealed partial class Checker
{
    private sealed class ObfuscationIndex
    {
        public ObfuscationIndex(
            Dictionary<string, ReservedEntry> exactEntries,
            Dictionary<string, ReservedEntry> compactEntries,
            IReadOnlyList<PartialEntry> partialEntries)
        {
            ExactEntries = exactEntries;
            CompactEntries = compactEntries;
            PartialEntries = partialEntries;
        }

        public Dictionary<string, ReservedEntry> ExactEntries { get; }
        public Dictionary<string, ReservedEntry> CompactEntries { get; }
        public IReadOnlyList<PartialEntry> PartialEntries { get; }
    }

    private readonly ObfuscationIndex _mainObfuscationIndex;
    private readonly ObfuscationIndex _customObfuscationIndex;
    private readonly ObfuscationIndex _identityObfuscationIndex;

    private ObfuscationIndex ResolveObfuscationIndex(
        IReadOnlyDictionary<string, ReservedEntry> exactEntries)
    {
        if (ReferenceEquals(exactEntries, _exact))
        {
            return _mainObfuscationIndex;
        }

        if (ReferenceEquals(exactEntries, _customExact))
        {
            return _customObfuscationIndex;
        }

        if (ReferenceEquals(exactEntries, _identityRuleExact))
        {
            return _identityObfuscationIndex;
        }

        throw new InvalidOperationException("Unknown obfuscation entry set.");
    }
}
