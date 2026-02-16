using System.Text;
using Listenfy.Application.Interfaces.Stats;
using Listenfy.Application.Settings;
using Listenfy.Domain;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Listenfy.Shared.Errors;
using Listenfy.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;

namespace Listenfy.Infrastructure.Services;

public class StatsService(ApplicationDbContext dbContext, TimeProvider timeProvider, IOptions<SpotifySettings> options, ILogger<StatsService> logger)
    : IStatsService
{
    private readonly SpotifySettings _spotifySettings = options.Value;

    public async Task<Result<UserWeeklyStatsDto>> GetUserWeeklyStats(ulong discordGuildId, ulong discordUserId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var userConnection = await dbContext
            .UserConnections.Include(uc => uc.SpotifyUser!)
            .FirstOrDefaultAsync(uc => uc.Guild.DiscordGuildId == discordGuildId && uc.DiscordUserId == discordUserId && uc.SpotifyUser != null);
        if (userConnection?.SpotifyUser is null)
        {
            logger.LogDebug("No Spotify connection found for user {UserId} in guild {GuildId}", discordUserId, discordGuildId);
            return Result<UserWeeklyStatsDto>.Failure(Errors.Stats.NotConnected);
        }

        var spotifyUser = userConnection.SpotifyUser;
        logger.LogInformation("Computing stats for user {UserId} in guild {GuildId} for last 7 days.", discordUserId, discordGuildId);

        var endDate = now;
        var startDate = now.AddDays(-7);

        // Check if we have any listening history older than the requested start date
        var hasOlderData = await dbContext.ListeningHistories.AnyAsync(lh => lh.SpotifyUserId == spotifyUser.Id && lh.PlayedAt < startDate);

        var weeklyStats = await ComputeUserWeeklyStats(spotifyUser, startDate, endDate);
        if (weeklyStats is null)
        {
            return Result<UserWeeklyStatsDto>.Failure(Errors.Stats.NoStatsAvailable(_spotifySettings.FetchDataJobIntervalInMinutes));
        }

        logger.LogInformation("Computed stats for user {UserId} from {StartDate} to {EndDate}", discordUserId, startDate, endDate);
        return Result<UserWeeklyStatsDto>.Success(
            new UserWeeklyStatsDto
            {
                DiscordUserId = discordUserId,
                WeekIdentifier = weeklyStats.WeekIdentifier,
                WeekStartDate = weeklyStats.WeekStartDate,
                WeekEndDate = weeklyStats.WeekEndDate,
                TopTracks = weeklyStats
                    .TopTracks.Select(t => new TopTrackDto
                    {
                        Name = t.Name,
                        Artist = t.Artist,
                        PlayCount = t.PlayCount,
                    })
                    .ToList(),
                TopArtists = weeklyStats.TopArtists.Select(a => new TopArtistDto { Name = a.Name, PlayCount = a.PlayCount }).ToList(),
                TotalMinutesListened = weeklyStats.TotalMinutesListened,
                TotalTracksPlayed = weeklyStats.TotalTracksPlayed,
                UniqueTracksPlayed = weeklyStats.UniqueTracksPlayed,
                IncludesEarliestData = !hasOlderData,
            }
        );
    }

    private async Task<WeeklyStat?> ComputeUserWeeklyStats(SpotifyUser spotifyUser, DateTime startDate, DateTime endDate)
    {
        var listeningHistory = await dbContext
            .ListeningHistories.Where(lh => lh.SpotifyUserId == spotifyUser.Id && lh.PlayedAt >= startDate && lh.PlayedAt <= endDate)
            .OrderBy(lh => lh.PlayedAt)
            .ToListAsync();
        if (listeningHistory.Count == 0)
        {
            logger.LogInformation(
                "No listening history for user {SpotifyUserId} between {StartDate} and {EndDate}.",
                spotifyUser.SpotifyUserId,
                startDate,
                endDate
            );
            return null;
        }

        // Use actual first and last track dates from the data
        var actualStartDate = listeningHistory.First().PlayedAt;
        var actualEndDate = listeningHistory.Last().PlayedAt;
        var weekIdentifier = $"Last 7 Days";

        logger.LogInformation(
            "Computing stats for user {SpotifyUserId}. Actual data range: {ActualStart} to {ActualEnd}",
            spotifyUser.SpotifyUserId,
            actualStartDate,
            actualEndDate
        );

        return Utilities.CalculateWeeklyStat(
            listeningHistory,
            spotifyUser.Id,
            timeProvider.GetUtcNow().UtcDateTime,
            weekIdentifier,
            actualStartDate,
            actualEndDate
        );
    }

    public async Task<Result<GuildWeeklyStatsDto>> GetGuildWeeklyStats(ulong discordGuildId)
    {
        var weekIdentifier = Utilities.CalculateWeekIdentifier(timeProvider.GetUtcNow().UtcDateTime);

        // Get all user connections for this guild
        var userConnections = await dbContext
            .UserConnections.Include(uc => uc.SpotifyUser!)
            .Where(uc => uc.Guild.DiscordGuildId == discordGuildId && uc.SpotifyUser != null)
            .ToListAsync();

        var spotifyUserIds = userConnections.Select(uc => uc.SpotifyUser!.Id).ToList();

        // Query WeeklyStats directly to ensure JSON columns are loaded
        var weeklyStatsForGuild = await dbContext
            .WeeklyStats.Where(ws => spotifyUserIds.Contains(ws.SpotifyUserId) && ws.WeekIdentifier == weekIdentifier)
            .ToListAsync();

        var usersWithStats = userConnections
            .Where(uc => weeklyStatsForGuild.Any(ws => ws.SpotifyUserId == uc.SpotifyUser!.Id))
            .Select(uc =>
            {
                var stats = weeklyStatsForGuild.First(ws => ws.SpotifyUserId == uc.SpotifyUser!.Id);
                return new UserWeeklyStatsDto
                {
                    DiscordUserId = uc.DiscordUserId,
                    WeekIdentifier = stats.WeekIdentifier,
                    WeekStartDate = stats.WeekStartDate,
                    WeekEndDate = stats.WeekEndDate,
                    TopTracks = stats
                        .TopTracks.Select(t => new TopTrackDto
                        {
                            Name = t.Name,
                            Artist = t.Artist,
                            PlayCount = t.PlayCount,
                        })
                        .ToList(),
                    TopArtists = stats.TopArtists.Select(a => new TopArtistDto { Name = a.Name, PlayCount = a.PlayCount }).ToList(),
                    TotalMinutesListened = stats.TotalMinutesListened,
                    TotalTracksPlayed = stats.TotalTracksPlayed,
                    UniqueTracksPlayed = stats.UniqueTracksPlayed,
                    IncludesEarliestData = false, // Guild stats use precomputed weekly data
                };
            })
            .OrderByDescending(u => u.TotalMinutesListened)
            .Take(StatMenuConstants.TOP_USERS_TO_SHOW)
            .ToList();

        if (usersWithStats.Count == 0)
        {
            logger.LogDebug("No weekly stats found for guild {GuildId}, week {WeekId}", discordGuildId, weekIdentifier);
            return Result<GuildWeeklyStatsDto>.Failure(Errors.Stats.NoStatsAvailable(_spotifySettings.FetchDataJobIntervalInMinutes));
        }

        var firstUser = usersWithStats.First();
        return Result<GuildWeeklyStatsDto>.Success(
            new GuildWeeklyStatsDto
            {
                WeekIdentifier = firstUser.WeekIdentifier,
                WeekStartDate = firstUser.WeekStartDate,
                WeekEndDate = firstUser.WeekEndDate,
                UserStats = usersWithStats,
            }
        );
    }

    public EmbedProperties BuildUserStatsEmbed(UserWeeklyStatsDto stats)
    {
        var description = new StringBuilder();
        description.AppendLine($"*{stats.WeekStartDate:MMM dd} - {stats.WeekEndDate:MMM dd, yyyy}*\n");

        description.AppendLine($"🎵 **{stats.TotalTracksPlayed}** tracks played ({stats.UniqueTracksPlayed} unique)");
        description.AppendLine($"⏱️ **{stats.TotalMinutesListened:N0}** minutes listened\n");

        if (stats.TopTracks.Count > 0)
        {
            description.AppendLine("**🔥 Top Tracks:**");
            for (var i = 0; i < Math.Min(5, stats.TopTracks.Count); i++)
            {
                var track = stats.TopTracks[i];
                description.AppendLine($"{i + 1}. **{track.Name}** by {track.Artist} ({track.PlayCount}x)");
            }

            description.AppendLine();
        }

        if (stats.TopArtists.Count > 0)
        {
            description.AppendLine("**🎤 Top Artists:**");
            for (var i = 0; i < Math.Min(5, stats.TopArtists.Count); i++)
            {
                var artist = stats.TopArtists[i];
                description.AppendLine($"{i + 1}. **{artist.Name}** ({artist.PlayCount}x)");
            }
        }

        // Add a note about data availability
        description.AppendLine();
        if (stats.IncludesEarliestData)
        {
            description.AppendLine("*\\* This includes all your available listening history since connecting.*");
        }
        else
        {
            description.AppendLine("*\\* Showing last 7 days. More history is available.*");
        }

        return new EmbedProperties
        {
            Title = $"📊 **Your Listening Stats - {stats.WeekIdentifier}**",
            Description = description.ToString(),
            Color = new Color(30, 215, 96), // Spotify green
            Timestamp = timeProvider.GetUtcNow().DateTime,
            Footer = new EmbedFooterProperties { Text = "Powered by Listenfy" },
        };
    }

    public EmbedProperties BuildGuildStatsEmbed(GuildWeeklyStatsDto guildStats)
    {
        var description = new StringBuilder();
        description.AppendLine($"📊 **Weekly Listening Stats - {guildStats.WeekIdentifier}**\n");
        description.AppendLine($"*{guildStats.WeekStartDate:MMM dd} - {guildStats.WeekEndDate:MMM dd, yyyy}*\n");

        foreach (var user in guildStats.UserStats)
        {
            var discordMention = $"<@{user.DiscordUserId}>";

            description.AppendLine($"**{discordMention}**");
            description.AppendLine($"🎵 {user.TotalTracksPlayed} tracks ({user.UniqueTracksPlayed} unique)");
            description.AppendLine($"⏱️ {user.TotalMinutesListened:N0} minutes\n");

            if (user.TopTracks.Count > 0)
            {
                var topTrack = user.TopTracks.First();
                description.AppendLine($"🔥 Top Track: **{topTrack.Name}** by {topTrack.Artist} ({topTrack.PlayCount}x plays)");
            }

            if (user.TopArtists.Count > 0)
            {
                var topArtist = user.TopArtists.First();
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
}
