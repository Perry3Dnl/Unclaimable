namespace Unclaimable.Extended;

/// <summary>Exposes deterministic counts for the embedded Extended snapshot.</summary>
public static class ExtendedData
{
    /// <summary>Total number of embedded Extended identifiers.</summary>
    public static int TotalEntries => ExtendedDataset.Entries.Value.Count;

    /// <summary>Returns the number of identifiers in one Extended category.</summary>
    public static int GetCount(ExtendedCategory category)
    {
        if (!Enum.IsDefined(typeof(ExtendedCategory), category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        return ExtendedDataset.Entries.Value.Count(entry => entry.Category == category);
    }
}
