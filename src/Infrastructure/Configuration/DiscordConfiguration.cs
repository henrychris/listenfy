using Listenfy.Application.Settings;
using Microsoft.Extensions.Options;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.ApplicationCommands;

namespace Listenfy.Infrastructure.Configuration;

internal static class DiscordConfiguration
{
    public static void SetupDiscord(this IServiceCollection services)
    {
        var discordSettings = services.BuildServiceProvider().GetService<IOptionsSnapshot<DiscordSettings>>()?.Value!;
        services.AddDiscordGateway(x => x.Token = discordSettings.BotToken).AddApplicationCommands();
    }
}
