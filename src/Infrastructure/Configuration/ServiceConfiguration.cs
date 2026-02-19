using Listenfy.Application.Interfaces;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Application.Interfaces.Stats;
using Listenfy.Infrastructure.Services;
using Listenfy.Infrastructure.Services.Spotify;
using Listenfy.Shared.Api.Handlers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Listenfy.Infrastructure.Configuration;

internal static class ServiceConfiguration
{
    /// <summary>
    /// Register services in the DI container.
    /// </summary>
    /// <param name="services"></param>
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<ISpotifyService, SpotifyService>();
        services.AddScoped<IStatsService, StatsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddTransient<RefitLoggingHandler>();

        // used for time manipulation and testing
        // we should use this instead of DateTime.Now
        services.TryAddSingleton(TimeProvider.System);
    }
}
