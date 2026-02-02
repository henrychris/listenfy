using System.Text;
using Listenfy.Application.Interfaces.Stats;
using Listenfy.Domain;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Listenfy.Shared.Errors;
using Listenfy.Shared.Results;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Rest;

namespace Listenfy.Infrastructure.Services;

public class StatsService(ApplicationDbContext dbContext, TimeProvider timeProvider, ILogger<StatsService> logger) : IStatsService
{
    public async Task<Result<UserWeeklyStatsDto>> GetUserWeeklyStats(ulong discordGuildId, ulong discordUserId)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (weekIdentifier, weekStart, weekEnd) = Utilities.CalculateCompletedWeek(now);

        var userConnection = await dbContext
            .UserConnections.Include(uc => uc.SpotifyUser!)
            .FirstOrDefaultAsync(uc => uc.Guild.DiscordGuildId == discordGuildId && uc.DiscordUserId == discordUserId && uc.SpotifyUser != null);
        if (userConnection?.SpotifyUser == null)
        {
            logger.LogDebug("No Spotify connection found for user {UserId} in guild {GuildId}", discordUserId, discordGuildId);
            return Result<UserWeeklyStatsDto>.Failure(Errors.Stats.NotConnected);
        }

        var spotifyUser = userConnection.SpotifyUser;

        // Check if precomputed stats exist - query directly from WeeklyStats to ensure JSON columns load
        var weeklyStats = await dbContext.WeeklyStats.FirstOrDefaultAsync(ws =>
            ws.SpotifyUserId == spotifyUser.Id && ws.WeekIdentifier == weekIdentifier
        );
        if (weeklyStats is null)
        {
            logger.LogInformation(
                "No precomputed stats found for user {UserId} in guild {GuildId}, week {WeekId}. Computing on the fly.",
                discordUserId,
                discordGuildId,
                weekIdentifier
            );

            // Compute stats on the fly
            weeklyStats = await ComputeUserWeeklyStats(spotifyUser, weekIdentifier, weekStart, weekEnd);
            if (weeklyStats is null)
            {
                return Result<UserWeeklyStatsDto>.Failure(Errors.Stats.NoStatsAvailable);
            }

            logger.LogInformation("Computed weekly stats on the fly for user {UserId} (not saving to DB)", discordUserId);
        }

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
            }
        );
    }

    private async Task<WeeklyStat?> ComputeUserWeeklyStats(SpotifyUser spotifyUser, string weekIdentifier, DateTime weekStart, DateTime weekEnd)
    {
        var listeningHistory = await dbContext
            .ListeningHistories.Where(lh => lh.SpotifyUserId == spotifyUser.Id && lh.PlayedAt >= weekStart && lh.PlayedAt <= weekEnd)
            .ToListAsync();
        if (listeningHistory.Count == 0)
        {
            logger.LogInformation(
                "No listening history for user {SpotifyUserId} in week {WeekIdentifier}.",
                spotifyUser.SpotifyUserId,
                weekIdentifier
            );
            return null;
        }

        return Utilities.CalculateWeeklyStat(
            listeningHistory,
            spotifyUser.Id,
            timeProvider.GetUtcNow().UtcDateTime,
            weekIdentifier,
            weekStart,
            weekEnd
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
                };
            })
            .OrderByDescending(u => u.TotalMinutesListened)
            .Take(StatMenuConstants.TOP_USERS_TO_SHOW)
            .ToList();

        if (usersWithStats.Count == 0)
        {
            logger.LogDebug("No weekly stats found for guild {GuildId}, week {WeekId}", discordGuildId, weekIdentifier);
            return Result<GuildWeeklyStatsDto>.Failure(Errors.Stats.NoStatsAvailable);
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
        description.AppendLine($"📊 **Your Weekly Stats - {stats.WeekIdentifier}**\n");
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

        return new EmbedProperties
        {
            Title = "🎧 Your Weekly Music Stats",
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
