using Unclaimable;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Provides dependency-injection registration for Unclaimable.</summary>
public static class UnclaimableServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configured <see cref="Options"/>, a live singleton <see cref="IPolicy"/>,
    /// and an <see cref="IChecker"/> that captures option values when it is constructed.
    /// Runtime policy updates remain visible to the registered checker.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional startup configuration for Unclaimable.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddUnclaimable(
        this IServiceCollection services,
        Action<Unclaimable.Options>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new Unclaimable.Options();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IPolicy>(_ =>
            new Policy(options.ConfiguredBlockedCharacters));
        services.AddSingleton<IChecker>(serviceProvider =>
            new Checker(
                options,
                serviceProvider.GetRequiredService<IPolicy>()));

        return services;
    }
}
