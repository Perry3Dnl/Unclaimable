using Unclaimable;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides dependency-injection registration helpers for Unclaimable.
/// </summary>
public static class UnclaimableServiceCollectionExtensions
{
    /// <summary>
    /// Registers Unclaimable options, the runtime character policy, and the identifier checker as singletons.
    /// </summary>
    /// <param name="services">The service collection to register Unclaimable with.</param>
    /// <param name="configure">An optional callback used to configure Unclaimable before services are registered.</param>
    /// <returns>The supplied service collection for fluent registration.</returns>
    public static IServiceCollection AddUnclaimable(
        this IServiceCollection services,
        Action<UnclaimableOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new UnclaimableOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IUnclaimablePolicy>(_ =>
            new UnclaimablePolicy(options.ConfiguredBlockedCharacters));
        services.AddSingleton<IUnclaimableChecker>(serviceProvider =>
            new UnclaimableChecker(
                options,
                serviceProvider.GetRequiredService<IUnclaimablePolicy>()));

        return services;
    }
}
