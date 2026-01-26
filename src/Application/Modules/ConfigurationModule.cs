using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Listenfy.Application.Modules;

public class ConfigurationModule(ApplicationDbContext dbContext, ILogger<ConfigurationModule> logger)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ping", "Ping!")]
    public static string Ping()
    {
        return "Pong!";
    }

    [SlashCommand("pong", "Pong!")]
    public static string Pong()
    {
        return "Ping!";
    }

    [SlashCommand("setchannel", "Invoke this in a channel to have weekly stats published here", DefaultGuildPermissions = Permissions.ManageGuild)]
    public async Task SetChannelAsync()
    {
        try
        {
            var guildId = Context.Interaction.GuildId;
            var user = Context.Interaction.User;

            if (!guildId.HasValue)
            {
                await BlockUsageOutsideServer();
                return;
            }

            // todo: experiment with channel menu for this command
            var settings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
            if (settings is null)
            {
                settings = new GuildSettings { DiscordGuildId = guildId.Value };
                dbContext.GuildSettings.Add(settings);
            }

            settings.StatsChannelId = Context.Channel.Id;
            await dbContext.SaveChangesAsync();
            await RespondAsync(InteractionCallback.Message($"✅ Weekly stats will now be posted in <#{Context.Channel.Id}>"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while setting stats channel");
            await RespondAsync(InteractionCallback.Message($"❌ An error occurred while setting the stats channel. Try again?"));
            throw;
        }
    }

    private async Task BlockUsageOutsideServer()
    {
        await RespondAsync(InteractionCallback.Message("❌ This command can only be used in a server!"));
    }
}
