namespace Unclaimable.Extended;

/// <summary>Registration helpers for the optional Extended data package.</summary>
public static class ExtendedOptionsExtensions
{
    /// <summary>
    /// Adds the selected Extended datasets to Core options using Core's configured
    /// reserved-identifier matching pipeline. Extended supplies data, not a matching engine.
    /// Merely installing Unclaimable.Extended does not change Core behavior.
    /// </summary>
    public static global::Unclaimable.Options UseExtendedData(
        this global::Unclaimable.Options options,
        Action<ExtendedOptions>? configure = null)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var extended = new ExtendedOptions();
        configure?.Invoke(extended);

        foreach (var entry in ExtendedDataset.Entries.Value)
        {
            if (!extended.IsEnabled(entry.Category) || extended.IsAllowed(entry.Value))
            {
                continue;
            }

            options.Reserve(
                entry.Value,
                entry.CategoryName,
                global::Unclaimable.ReservedMatchMode.Default);
        }

        return options;
    }
}
