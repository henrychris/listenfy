using Listenfy.Domain;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
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

    [SlashCommand("help", "Get help with Listenfy commands")]
    public static InteractionMessageProperties Help()
    {
        var embeds = new[]
        {
            new EmbedProperties
            {
                Title = "🎵 Listenfy Help",
                Description = "Here's what you can do with Listenfy!",
                Color = new Color(30, 215, 96), // Spotify Green
                Fields =
                [
                    new EmbedFieldProperties
                    {
                        Name = "🚀 First Time Setup",
                        Value =
                            "1. Use `/connect` to link your Spotify account\n2. A server admin should use `/setchannel` to choose where weekly stats are posted\n3. Come back anytime to check `/personalstats` or `/serverstats`",
                    },
                    new EmbedFieldProperties
                    {
                        Name = "🎧 Personal Commands",
                        Value =
                            "`/connect` - Link your Spotify account to this server\n`/disconnect` - Unlink your Spotify account from this server\n`/personalstats` - View your weekly listening statistics",
                    },
                    new EmbedFieldProperties
                    {
                        Name = "🏆 Server Commands",
                        Value =
                            "`/serverstats` - See the top listeners in this server\n`/setchannel` - (Admin) Set where weekly stats are posted\n`/getchannel` - (Admin) See the current stats channel\n`/clearchannel` - (Admin) Remove the stats channel",
                    },
                    new EmbedFieldProperties { Name = "❓ Other", Value = "`/ping` - Pong!\n`/pong` - Ping!" },
                ],
            },
        };

        return new InteractionMessageProperties { Embeds = embeds };
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
            var message = new InteractionMessageProperties
            {
                Content = ChannelMenuConstants.PROMPT,
                Components =
                [
                    new ChannelMenuProperties(ChannelMenuConstants.CUSTOM_ID)
                    {
                        Placeholder = ChannelMenuConstants.PLACEHOLDER,
                        MinValues = 1,
                        MaxValues = 1,
                        ChannelTypes = [ChannelType.TextGuildChannel],
                    },
                ],
            };

            await RespondAsync(InteractionCallback.Message(message));
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
}
