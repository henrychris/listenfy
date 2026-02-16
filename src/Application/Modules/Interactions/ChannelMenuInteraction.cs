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
                logger.LogWarning("SetChannelFromMenu command invoked outside of a server");
                await InteractionGuards.BlockUsageOutsideServerAsync(Context);
                return;
            }

            logger.LogInformation(
                "SetChannelFromMenu command started. GuildId: {GuildId}, UserId: {UserId}, ChannelId: {ChannelId}",
                guildId.Value,
                user.Id,
                Context.Channel.Id
            );

            var settings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
            if (settings is null)
            {
                logger.LogInformation("Creating new GuildSettings for GuildId: {GuildId}", guildId.Value);
                settings = new GuildSettings { DiscordGuildId = guildId.Value, GuildName = Context.Guild?.Name ?? "Unknown Guild" };
                dbContext.GuildSettings.Add(settings);
            }
            else
            {
                logger.LogInformation("Updating existing GuildSettings for GuildId: {GuildId}", guildId.Value);
            }

            var selectedChannel = Context.SelectedValues[0];
            if (selectedChannel is null)
            {
                await RespondAsync(InteractionCallback.Message("❌ Select a channel."));
                return;
            }

            settings.StatsChannelId = selectedChannel.Id;
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Stats channel successfully set to {ChannelId} for GuildId: {GuildId}", selectedChannel.Id, guildId.Value);

            await RespondAsync(InteractionCallback.Message($"✅ Weekly stats will now be posted in <#{selectedChannel.Id}>"));
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
}
