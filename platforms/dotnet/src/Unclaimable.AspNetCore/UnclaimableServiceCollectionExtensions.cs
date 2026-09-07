using Unclaimable;

namespace Microsoft.Extensions.DependencyInjection;

public static class UnclaimableServiceCollectionExtensions
{
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
