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
                logger.LogWarning("SetChannel command invoked outside of a server");
                await InteractionGuards.BlockUsageOutsideServerAsync(Context);
                return;
            }

            logger.LogInformation(
                "SetChannel command started. GuildId: {GuildId}, UserId: {UserId}, ChannelId: {ChannelId}",
                guildId.Value,
                user.Id,
                Context.Channel.Id
            );

            // todo: experiment with channel menu for this command
            var settings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
            if (settings is null)
            {
                logger.LogInformation("Creating new GuildSettings for GuildId: {GuildId}", guildId.Value);
                settings = new GuildSettings { DiscordGuildId = guildId.Value };
                dbContext.GuildSettings.Add(settings);
            }
            else
            {
                logger.LogInformation("Updating existing GuildSettings for GuildId: {GuildId}", guildId.Value);
            }

            settings.StatsChannelId = Context.Channel.Id;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Stats channel successfully set to {ChannelId} for GuildId: {GuildId}", Context.Channel.Id, guildId.Value);
            await RespondAsync(InteractionCallback.Message($"✅ Weekly stats will now be posted in <#{Context.Channel.Id}>"));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while setting stats channel. GuildId: {GuildId}, UserId: {UserId}",
                Context.Interaction.GuildId?.ToString() ?? "null",
                Context.Interaction.User.Id
            );
            await RespondAsync(InteractionCallback.Message($"❌ An error occurred while setting the stats channel. Try again?"));
            throw;
        }
    }

    [SlashCommand("getchannel", "See the channel where weekly statistics are sent", DefaultGuildPermissions = Permissions.ManageGuild)]
    public async Task GetChannelAsync()
    {
        try
        {
            var guildId = Context.Interaction.GuildId;
            var user = Context.Interaction.User;

            if (!guildId.HasValue)
            {
                logger.LogWarning("GetChannel command invoked outside of a server");
                await InteractionGuards.BlockUsageOutsideServerAsync(Context);
                return;
            }

            logger.LogInformation("GetChannel command started. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, user.Id);

            var settings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
            if (settings is null)
            {
                logger.LogInformation("No GuildSettings found for GuildId: {GuildId}", guildId.Value);
                await RespondAsync(InteractionCallback.Message($"❌ This server has no stats channel set. Use `/setchannel`"));
                return;
            }

            if (settings.StatsChannelId is null)
            {
                await RespondAsync(InteractionCallback.Message($"❌ This server has no stats channel set. Use `/setchannel`"));
                return;
            }

            logger.LogInformation("Retrieved stats channel {ChannelId} for GuildId: {GuildId}", settings.StatsChannelId, guildId.Value);
            await RespondAsync(InteractionCallback.Message($"✅ Weekly stats are currently being sent to <#{settings.StatsChannelId}>"));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while getting stats channel. GuildId: {GuildId}, UserId: {UserId}",
                Context.Interaction.GuildId?.ToString() ?? "null",
                Context.Interaction.User.Id
            );
            await RespondAsync(InteractionCallback.Message($"❌ An error occurred while getting the stats channel. Try again?"));
            throw;
        }
    }

    [SlashCommand(
        "clearchannel",
        "Clear the channel where weekly statistics are sent. Weekly stats will not be sent.",
        DefaultGuildPermissions = Permissions.ManageGuild
    )]
    public async Task ClearChannelAsync()
    {
        try
        {
            var guildId = Context.Interaction.GuildId;
            var user = Context.Interaction.User;

            if (!guildId.HasValue)
            {
                logger.LogWarning("ClearChannel command invoked outside of a server");
                await InteractionGuards.BlockUsageOutsideServerAsync(Context);
                return;
            }

            logger.LogInformation("ClearChannel command started. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, user.Id);

            var settings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
            if (settings is null)
            {
                logger.LogInformation("No GuildSettings found for clearing. GuildId: {GuildId}", guildId.Value);
                await RespondAsync(InteractionCallback.Message($"❌ This server has no stats channel set. Use `/setchannel`"));
                return;
            }

            logger.LogInformation(
                "Clearing stats channel for GuildId: {GuildId}. Previous channel: {ChannelId}",
                guildId.Value,
                settings.StatsChannelId
            );

            settings.StatsChannelId = null;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Stats channel successfully cleared for GuildId: {GuildId}", guildId.Value);
            await RespondAsync(
                InteractionCallback.Message($"✅ Stats channel has been cleared. Weekly stats will not be sent until you run `/setchannel` again.")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while clearing stats channel. GuildId: {GuildId}, UserId: {UserId}",
                Context.Interaction.GuildId?.ToString() ?? "null",
                Context.Interaction.User.Id
            );
            await RespondAsync(InteractionCallback.Message($"❌ An error occurred while clearing the stats channel. Try again?"));
            throw;
        }
    }

    private async Task BlockUsageOutsideServer()
    {
        await RespondAsync(InteractionCallback.Message("❌ This command can only be used in a server!"));
    }
}
