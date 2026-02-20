using Listenfy.Application.Interfaces.Stats;
using Listenfy.Domain.Models;
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
        logger.LogInformation("Found guilds with stats enabled. Context: {@Context}", new { NumberOfGuilds = guildsWithStats.Count });

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
                logger.LogError(ex, "Error sending weekly stats to guild. Context: {@Context}", new { GuildId = guild.DiscordGuildId });
            }
        }

        logger.LogInformation("Completed SendWeeklyStatsJob. Context: {@Context}", new { Sent = sentCount, Errors = errorCount });
    }

    private async Task<bool> SendGuildWeeklyStats(GuildSettings guild)
    {
        var guildStatsResult = await statsService.GetGuildWeeklyStats(guild.DiscordGuildId);
        if (guildStatsResult.IsFailure)
        {
            logger.LogError(
                "No users with stats for guild. Context: {@Context}",
                new { GuildId = guild.DiscordGuildId, Reason = guildStatsResult.Error.Description }
            );
            return false;
        }

        var guildStats = guildStatsResult.Value;
        var embed = statsService.BuildGuildStatsEmbed(guildStats, false);

        try
        {
            var channel = await discordClient.Rest.GetChannelAsync(guild.StatsChannelId!.Value);
            if (channel is not TextChannel textChannel)
            {
                logger.LogError(
                    "Stats channel is not a text channel in guild. Context: {@Context}",
                    new { ChannelId = guild.StatsChannelId, GuildId = guild.DiscordGuildId }
                );
                return false;
            }

            await textChannel.SendMessageAsync(new MessageProperties { Embeds = [embed] });
            logger.LogInformation(
                "Sent weekly stats to guild. Context: {@Context}",
                new
                {
                    GuildId = guild.DiscordGuildId,
                    ChannelId = guild.StatsChannelId,
                    UserCount = guildStats.UserStats.Count,
                }
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send message to channel. Context: {@Context}",
                new { ChannelId = guild.StatsChannelId, GuildId = guild.DiscordGuildId }
            );
            return false;
        }
    }
}
