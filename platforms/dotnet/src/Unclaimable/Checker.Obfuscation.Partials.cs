namespace Unclaimable;

public sealed partial class Checker
{
    private bool TryMatchObfuscationPartials(
        string value,
        IReadOnlyList<PartialEntry> partialEntries,
        bool preserveNonCompactCharacters,
        bool directPotential,
        HashSet<long> failedStates,
        out ReservedEntry? match,
        out int? matchStartIndex,
        out int? matchLength)
    {
        foreach (var partial in partialEntries)
        {
            var target = preserveNonCompactCharacters ? partial.Exact : partial.Compact;
            var editBudget = GetObfuscationEditBudget(partial.Entry, target.Length);

            if (editBudget == 0 && !directPotential)
            {
                continue;
            }

            if (value.Length <= target.Length - editBudget)
            {
                continue;
            }

            for (var start = 0; start < value.Length; start++)
            {
                int endIndex;
                if (!TryMatchObfuscationText(
                        value,
                        start,
                        target,
                        preserveNonCompactCharacters,
                        editBudget,
                        requireSourceEnd: false,
                        failedStates,
                        out endIndex))
                {
                    continue;
                }

                match = partial.Entry;
                matchStartIndex = start;
                matchLength = Math.Max(1, endIndex - start);
                return true;
            }
        }

        match = null;
        matchStartIndex = null;
        matchLength = null;
        return false;
    }
}
