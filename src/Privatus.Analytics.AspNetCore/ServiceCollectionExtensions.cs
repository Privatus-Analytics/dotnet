using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Privatus.Analytics;

/// <summary>Registers Privatus Analytics with dependency injection.</summary>
public static class PrivatusAnalyticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds Privatus Analytics. Settings are read from the PrivatusAnalytics
    /// configuration section, then from <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddPrivatusAnalytics(this IServiceCollection services, Action<PrivatusAnalyticsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddOptions<PrivatusAnalyticsOptions>().BindConfiguration(PrivatusAnalyticsOptions.SectionName);
        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.TryAddSingleton<PrivatusAnalyticsService>();
        return services;
    }

    /// <summary>Adds Privatus Analytics with settings read from the given configuration section.</summary>
    public static IServiceCollection AddPrivatusAnalytics(this IServiceCollection services, IConfiguration configuration, Action<PrivatusAnalyticsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services.AddOptions<PrivatusAnalyticsOptions>().Bind(configuration);
        if (configure is not null)
        {
            builder.Configure(configure);
        }

        services.TryAddSingleton<PrivatusAnalyticsService>();
        return services;
    }
}
