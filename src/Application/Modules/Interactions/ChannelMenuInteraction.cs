using Listenfy.Domain;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Listenfy.Application.Modules.Interactions;

/// <summary>
/// This interaction shows users a dropdown menu after they run the `setchannel` command. Follow where `ChannelMenuConstants.CUSTOM_ID` is used in the code to see how this interaction is triggered.
/// </summary>
public class ChannelMenuInteraction(ApplicationDbContext dbContext, ILogger<ConfigurationModule> logger)
    : ComponentInteractionModule<ChannelMenuInteractionContext>
{
    [ComponentInteraction(ChannelMenuConstants.CUSTOM_ID)]
    public async Task SetChannelFromMenu()
    {
        try
        {
            var guildId = Context.Interaction.GuildId;
            var user = Context.Interaction.User;

            if (!guildId.HasValue)
            {
                logger.LogError(
                    "SetChannelFromMenu command invoked outside of a server. Context: {@Context}",
                    new
                    {
                        DiscordUserId = user.Id,
                        GuildId = guildId,
                        ChannelId = Context.Channel.Id,
                    }
                );
                await InteractionGuards.BlockUsageOutsideServerAsync(Context);
                return;
            }

            logger.LogInformation(
                "SetChannelFromMenu command started. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = user.Id,
                    ChannelId = Context.Channel.Id,
                }
            );

            var settings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
            if (settings is null)
            {
                logger.LogInformation(
                    "Creating new GuildSettings. Context: {@Context}",
                    new { GuildId = guildId.Value, GuildName = Context.Guild?.Name ?? "Unknown Guild" }
                );
                settings = new GuildSettings { DiscordGuildId = guildId.Value, GuildName = Context.Guild?.Name ?? "Unknown Guild" };
                dbContext.GuildSettings.Add(settings);
            }
            else
            {
                logger.LogInformation("Existing GuildSettings found. Context: {@Context}", new { GuildId = guildId.Value, settings.GuildName });
            }

            var selectedChannel = Context.SelectedValues[0];
            if (selectedChannel is null)
            {
                logger.LogError(
                    "No channel selected in dropdown. Context: {@Context}",
                    new
                    {
                        GuildId = guildId.Value,
                        DiscordUserId = user.Id,
                        ChannelId = Context.Channel.Id,
                    }
                );
                await RespondAsync(InteractionCallback.Message("❌ Select a channel."));
                return;
            }

            settings.StatsChannelId = selectedChannel.Id;
            await dbContext.SaveChangesAsync();
            logger.LogInformation(
                "Stats channel successfully set. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = user.Id,
                    ChannelId = selectedChannel.Id,
                }
            );

            await RespondAsync(InteractionCallback.Message($"✅ Weekly stats will now be posted in <#{selectedChannel.Id}>"));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while setting stats channel. Context: {@Context}",
                new
                {
                    GuildId = Context.Interaction.GuildId?.ToString() ?? "null",
                    DiscordUserId = Context.Interaction.User.Id,
                    ChannelId = Context.Channel.Id,
                }
            );
            await RespondAsync(InteractionCallback.Message($"❌ An error occurred while setting the stats channel. Try again?"));
            throw;
        }
    }
}
