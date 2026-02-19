using Listenfy.Application.Interfaces.Stats;
using Listenfy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Listenfy.Application.Jobs;

public class SendWeeklyStatsJob(
    ApplicationDbContext dbContext,
    GatewayClient discordClient,
    IStatsService statsService,
    ILogger<SendWeeklyStatsJob> logger
)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting SendWeeklyStatsJob");

        // Get all guilds with enabled stats and configured channels
        var guildsWithStats = await dbContext.GuildSettings.Where(g => g.IsEnabled && g.StatsChannelId != null).ToListAsync();
        logger.LogInformation("Found {Count} guilds with stats enabled", guildsWithStats.Count);

        var sentCount = 0;
        var errorCount = 0;

        foreach (var guild in guildsWithStats)
        {
            try
            {
                var sent = await SendGuildWeeklyStats(guild);
                if (sent)
                {
                    sentCount++;
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                logger.LogError(ex, "Error sending weekly stats to guild {GuildId}", guild.DiscordGuildId);
            }
        }

        logger.LogInformation("Completed SendWeeklyStatsJob. Sent: {Sent}, Errors: {Errors}", sentCount, errorCount);
    }

    private async Task<bool> SendGuildWeeklyStats(Domain.Models.GuildSettings guild)
    {
        var guildStatsResult = await statsService.GetGuildWeeklyStats(guild.DiscordGuildId);
        if (guildStatsResult.IsFailure)
        {
            logger.LogDebug("No users with stats for guild {GuildId}. Reason: {Reason}", guild.DiscordGuildId, guildStatsResult.Error.Description);
            return false;
        }

        var guildStats = guildStatsResult.Value;
        var embed = statsService.BuildGuildStatsEmbed(guildStats, false);

        try
        {
            var channel = await discordClient.Rest.GetChannelAsync(guild.StatsChannelId!.Value);
            if (channel is not TextChannel textChannel)
            {
                logger.LogWarning("Stats channel {ChannelId} is not a text channel in guild {GuildId}", guild.StatsChannelId, guild.DiscordGuildId);
                return false;
            }

            await textChannel.SendMessageAsync(new MessageProperties { Embeds = [embed] });
            logger.LogInformation(
                "Sent weekly stats to guild {GuildId}, channel {ChannelId}, users: {UserCount}",
                guild.DiscordGuildId,
                guild.StatsChannelId,
                guildStats.UserStats.Count
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to channel {ChannelId} in guild {GuildId}", guild.StatsChannelId, guild.DiscordGuildId);
            return false;
        }
    }
}
