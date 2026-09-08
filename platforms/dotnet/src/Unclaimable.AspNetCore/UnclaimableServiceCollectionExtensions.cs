using Unclaimable;

namespace Microsoft.Extensions.DependencyInjection;

public static class UnclaimableServiceCollectionExtensions
{
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
