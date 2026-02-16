using System.Text.Json;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Application.Settings;
using Listenfy.Shared.Api.Handlers;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Refit;

namespace Listenfy.Infrastructure.Configuration;

internal static class ExternalServiceConfiguration
{
    public static void SetupDiscord(this IServiceCollection services)
    {
        var discordSettings = services.BuildServiceProvider().GetService<IOptions<DiscordSettings>>()?.Value!;
        services
            .AddDiscordGateway(x => x.Token = discordSettings.BotToken)
            .AddComponentInteractions<ChannelMenuInteraction, ChannelMenuInteractionContext>()
            .AddApplicationCommands();
    }

    public static void SetupSpotify(this IServiceCollection services)
    {
        var spotifySettings = services.BuildServiceProvider().GetService<IOptions<SpotifySettings>>()?.Value!;
        services
            .AddRefitClient<ISpotifyApi>(
                new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
                    ),
                }
            )
            .ConfigureHttpClient(
                (serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<IOptions<SpotifySettings>>().Value;
                    client.BaseAddress = new Uri(settings.ApiBaseUrl);
                }
            )
            .AddHttpMessageHandler<RefitLoggingHandler>()
            .AddStandardResilienceHandler();

        services
            .AddRefitClient<ISpotifyAccountApi>(
                new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
                    ),
                }
            )
            .ConfigureHttpClient(
                (serviceProvider, client) =>
                {
                    var settings = serviceProvider.GetRequiredService<IOptions<SpotifySettings>>().Value;
                    client.BaseAddress = new Uri(settings.AccountsBaseUrl);
                }
            )
            .AddHttpMessageHandler<RefitLoggingHandler>()
            .AddStandardResilienceHandler();
    }
}
