using System.Globalization;
using System.Text;
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
    TimeProvider timeProvider,
    ILogger<SendWeeklyStatsJob> logger
)
{
    private const int TOP_USERS_TO_SHOW = 5;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting SendWeeklyStatsJob");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var weekIdentifier = CalculateWeekIdentifier(now);

        logger.LogInformation("Sending stats for week {WeekIdentifier}", weekIdentifier);

        // Get all guilds with enabled stats and configured channels
        var guildsWithStats = await dbContext.GuildSettings.Where(g => g.IsEnabled && g.StatsChannelId != null).ToListAsync();
        logger.LogInformation("Found {Count} guilds with stats enabled", guildsWithStats.Count);

        var sentCount = 0;
        var errorCount = 0;

        foreach (var guild in guildsWithStats)
        {
            try
            {
                var sent = await SendGuildWeeklyStats(guild, weekIdentifier);
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

    private async Task<bool> SendGuildWeeklyStats(GuildSettings guild, string weekIdentifier)
    {
        // Get all users connected to this guild with their weekly stats
        var userConnections = await dbContext
            .UserConnections.Include(uc => uc.SpotifyUser!)
            .ThenInclude(su => su.WeeklyStats)
            .Where(uc => uc.Guild.DiscordGuildId == guild.DiscordGuildId && uc.SpotifyUser != null)
            .ToListAsync();
        if (userConnections.Count == 0)
        {
            logger.LogDebug("No users with Spotify connected in guild {GuildId}", guild.DiscordGuildId);
            return false;
        }

        // Filter to users who have stats for this week
        var usersWithStats = userConnections.Where(uc => uc.SpotifyUser?.WeeklyStats.Any(ws => ws.WeekIdentifier == weekIdentifier) == true).ToList();
        if (usersWithStats.Count == 0)
        {
            logger.LogDebug("No users with stats for week {WeekIdentifier} in guild {GuildId}", weekIdentifier, guild.DiscordGuildId);
            return false;
        }

        var embed = BuildWeeklyStatsEmbed(usersWithStats, weekIdentifier);
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
                usersWithStats.Count
            );
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to channel {ChannelId} in guild {GuildId}", guild.StatsChannelId, guild.DiscordGuildId);
            return false;
        }
    }

    private EmbedProperties BuildWeeklyStatsEmbed(List<UserConnection> usersWithStats, string weekIdentifier)
    {
        var description = new StringBuilder();
        description.AppendLine($"📊 **Weekly Listening Stats - {weekIdentifier}**\n");

        // Get the week date range from first user's stats
        var firstUserStats = usersWithStats.First().SpotifyUser!.WeeklyStats.First(ws => ws.WeekIdentifier == weekIdentifier);
        description.AppendLine($"*{firstUserStats.WeekStartDate:MMM dd} - {firstUserStats.WeekEndDate:MMM dd, yyyy}*\n");

        // Sort by total minutes listened and take top 5
        var sortedUsers = usersWithStats
            .Select(uc => new { UserConnection = uc, Stats = uc.SpotifyUser!.WeeklyStats.First(ws => ws.WeekIdentifier == weekIdentifier) })
            .OrderByDescending(x => x.Stats.TotalMinutesListened)
            .Take(TOP_USERS_TO_SHOW)
            .ToList();

        foreach (var user in sortedUsers)
        {
            var stats = user.Stats;
            var discordMention = $"<@{user.UserConnection.DiscordUserId}>";

            description.AppendLine($"**{discordMention}**");
            description.AppendLine($"🎵 {stats.TotalTracksPlayed} tracks ({stats.UniqueTracksPlayed} unique)");
            description.AppendLine($"⏱️ {stats.TotalMinutesListened:N0} minutes\n");

            if (stats.TopTracks.Count > 0)
            {
                var topTrack = stats.TopTracks.First();
                description.AppendLine($"🔥 Top Track: **{topTrack.Name}** by {topTrack.Artist} ({topTrack.PlayCount}x plays)");
            }

            if (stats.TopArtists.Count > 0)
            {
                var topArtist = stats.TopArtists.First();
                description.AppendLine($"🎤 Top Artist: **{topArtist.Name}** ({topArtist.PlayCount}x plays)");
            }

            description.AppendLine();
        }

        return new EmbedProperties
        {
            Title = "🎧 Weekly Music Roundup",
            Description = description.ToString(),
            Color = new Color(30, 215, 96), // Spotify green
            Timestamp = timeProvider.GetUtcNow().DateTime,
            Footer = new EmbedFooterProperties { Text = "Powered by Listenfy" },
        };
    }

    private static string CalculateWeekIdentifier(DateTime now)
    {
        // Same logic as AggregateWeeklyStatsJob
        var daysSinceMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (daysSinceMonday == 0 && now.TimeOfDay.TotalHours < 9)
        {
            daysSinceMonday = 7;
        }

        var weekStart = now.Date.AddDays(-daysSinceMonday);

        var calendar = CultureInfo.InvariantCulture.Calendar;
        var weekNumber = calendar.GetWeekOfYear(weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var year = weekStart.Year;

        if (weekNumber >= 52 && weekStart.Month == 1)
        {
            year--;
        }

        return $"{year}-W{weekNumber:D2}";
    }
}
