using System.Globalization;
using Listenfy.Application.Interfaces;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Listenfy.Application.Jobs;

public class FetchListeningDataJob(
    ApplicationDbContext dbContext,
    ISpotifyService spotifyService,
    INotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<FetchListeningDataJob> logger
)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting FetchListeningDataJob");

        var spotifyUsers = await dbContext
            .SpotifyUsers.Include(u => u.SpotifyFetchMetadata)
            .Include(u => u.UserConnections)
            .ThenInclude(uc => uc.Guild)
            .ToListAsync();
        logger.LogInformation("Found Spotify users to process. Context: {@Context}", new { NumberOfUsers = spotifyUsers.Count });

        foreach (var user in spotifyUsers)
        {
            if (user.UserConnections.Count == 0)
            {
                // ignore users whose accounts are disconnected so we don't send multiple disconnection DMs
                continue;
            }

            try
            {
                await ProcessUserListeningHistory(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing listening history for SpotifyUser. Context: {@Context}", new { user.SpotifyUserId });
            }
        }

        logger.LogInformation("Completed FetchListeningDataJob");
    }

    public async Task ExecuteForUserAsync(string spotifyUserId)
    {
        logger.LogInformation("Starting FetchListeningDataJob for user. Context: {@Context}", new { SpotifyUserId = spotifyUserId });

        var user = await dbContext
            .SpotifyUsers.Include(u => u.SpotifyFetchMetadata)
            .Include(u => u.UserConnections)
            .ThenInclude(uc => uc.Guild)
            .FirstOrDefaultAsync(u => u.Id == spotifyUserId);
        if (user is null)
        {
            logger.LogWarning("SpotifyUser not found. Context: {@Context}", new { SpotifyUserId = spotifyUserId });
            return;
        }

        if (user.UserConnections.Count == 0)
        {
            // ignore users whose accounts are disconnected so we don't send multiple disconnection DMs
            return;
        }

        try
        {
            await ProcessUserListeningHistory(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing listening history for SpotifyUser. Context: {@Context}", new { user.SpotifyUserId });
        }

        logger.LogInformation("Completed FetchListeningDataJob for user. Context: {@Context}", new { SpotifyUserId = spotifyUserId });
    }

    private async Task ProcessUserListeningHistory(SpotifyUser user)
    {
        logger.LogInformation("Processing user. Context: {@Context}", new { user.SpotifyUserId });

        var metadata = user.SpotifyFetchMetadata;
        if (metadata is null)
        {
            await ProcessFirstTimeFetch(user);
        }
        else
        {
            await ProcessSubsequentFetch(user, metadata);
        }
    }

    private async Task ProcessFirstTimeFetch(SpotifyUser user)
    {
        logger.LogInformation("First-time fetch for user. Context: {@Context}", new { user.SpotifyUserId });

        var result = await spotifyService.GetRecentlyPlayedTracks(user);
        if (result.IsFailure)
        {
            // Check if this is an expired refresh token
            if (result.Error.Code == Errors.Spotify.RefreshTokenExpired.Code)
            {
                await HandleExpiredRefreshToken(user);
                return;
            }

            logger.LogError(
                "Failed to fetch recently played tracks for user. Context: {@Context}.",
                new { user.SpotifyUserId, Error = result.Error.Description }
            );
            return;
        }

        var response = result.Value;
        var items = response.Items;
        if (items.Count == 0)
        {
            logger.LogWarning("No tracks available for first-time fetch for user. Context: {@Context}", new { user.SpotifyUserId });

            var metadata = new SpotifyFetchMetadata
            {
                SpotifyUserId = user.Id,
                LastFetchedAt = timeProvider.GetUtcNow().UtcDateTime,
                TracksFetchedInLastRun = 0,
            };
            dbContext.SpotifyFetchMetadata.Add(metadata);
            await dbContext.SaveChangesAsync();
            return;
        }

        logger.LogInformation(
            "Found tracks for first-time fetch for user. Context: {@Context}",
            new { user.SpotifyUserId, NumberOfTracks = items.Count }
        );
        var listeningHistories = await BuildNewListeningHistories(user, items);
        if (listeningHistories.Count == 0)
        {
            logger.LogInformation("All fetched tracks already stored for user. Context: {@Context}", new { user.SpotifyUserId });
        }
        else
        {
            await dbContext.ListeningHistories.AddRangeAsync(listeningHistories);
        }

        var newMetadata = new SpotifyFetchMetadata
        {
            SpotifyUserId = user.Id,
            LastFetchedAt = GetLastFetchedTimestamp(response, items),
            TracksFetchedInLastRun = listeningHistories.Count,
        };
        dbContext.SpotifyFetchMetadata.Add(newMetadata);

        await dbContext.SaveChangesAsync();
        logger.LogInformation(
            "Saved tracks for user. Context: {@Context}",
            new
            {
                user.SpotifyUserId,
                NumberOfTracks = listeningHistories.Count,
                newMetadata.LastFetchedAt,
            }
        );
    }

    private async Task ProcessSubsequentFetch(SpotifyUser user, SpotifyFetchMetadata metadata)
    {
        var afterTimestampMilliseconds = new DateTimeOffset(metadata.LastFetchedAt).ToUnixTimeMilliseconds();
        logger.LogInformation(
            "Fetching tracks for user. Context: {@Context}",
            new
            {
                user.SpotifyUserId,
                AfterTimestampInMilliseconds = afterTimestampMilliseconds,
                metadata.LastFetchedAt,
            }
        );

        var result = await spotifyService.GetRecentlyPlayedTracks(user, afterTimestampMilliseconds);
        if (result.IsFailure)
        {
            // Check if this is an expired refresh token
            if (result.Error.Code == Errors.Spotify.RefreshTokenExpired.Code)
            {
                await HandleExpiredRefreshToken(user);
                return;
            }

            logger.LogError(
                "Failed to fetch recently played tracks for user. Context: {@Context}.",
                new { user.SpotifyUserId, Error = result.Error.Description }
            );
            return;
        }

        var response = result.Value;
        var items = response.Items;
        if (items.Count == 0)
        {
            logger.LogWarning("No new tracks for user. Context: {@Context}", new { user.SpotifyUserId });
            // Don't update LastFetchedAt if there are no new tracks - keep using the same cursor
            metadata.TracksFetchedInLastRun = 0;
            await dbContext.SaveChangesAsync();
            return;
        }

        logger.LogInformation("Found new tracks for user. Context: {@Context}", new { user.SpotifyUserId, NumberOfTracks = items.Count });

        // Save tracks to listening history
        var listeningHistories = await BuildNewListeningHistories(user, items);
        if (listeningHistories.Count == 0)
        {
            logger.LogInformation("All fetched tracks already stored for user. Context: {@Context}", new { user.SpotifyUserId });
        }
        else
        {
            await dbContext.ListeningHistories.AddRangeAsync(listeningHistories);
        }

        metadata.LastFetchedAt = GetLastFetchedTimestamp(response, items);
        metadata.TracksFetchedInLastRun = listeningHistories.Count;
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Saved tracks for user. Context: {@Context}",
            new
            {
                user.SpotifyUserId,
                NumberOfTracks = listeningHistories.Count,
                metadata.LastFetchedAt,
            }
        );
    }

    private async Task HandleExpiredRefreshToken(SpotifyUser user)
    {
        logger.LogWarning("Refresh token expired for SpotifyUser. Removing user connections. Context: {@Context}.", new { user.SpotifyUserId });

        // Get all connections for this user (they may be connected to multiple guilds)
        // todo: send a single DM if connected to multiple guilds instead of one DM per guild - would require grouping connections by DiscordUserId and aggregating guild info for the DM
        var connections = user.UserConnections.ToList();
        foreach (var connection in connections)
        {
            var guildName = connection.Guild?.GuildName ?? "Unknown Server";
            logger.LogInformation(
                "Notifying Discord user about expired token in guild. Context: {@Context}",
                new
                {
                    connection.DiscordUserId,
                    GuildName = guildName,
                    GuildId = connection.Guild?.DiscordGuildId ?? 0,
                }
            );

            // Notify the user via DM
            await notificationService.NotifyRefreshTokenExpiredAsync(connection.DiscordUserId, guildName);
        }

        // Remove all connections for this user
        dbContext.UserConnections.RemoveRange(connections);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Removed SpotifyUser. Context: {@Context}", new { user.SpotifyUserId });
    }

    private DateTime GetLastFetchedTimestamp(SpotifyRecentlyPlayedTracksResponse response, List<SpotifyRecentlyPlayedItem> items)
    {
        if (!string.IsNullOrEmpty(response.Cursors.After))
        {
            var cursorTimestamp = long.Parse(response.Cursors.After);
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cursorTimestamp).UtcDateTime;
            logger.LogInformation("Using cursor timestamp. Context: {@Context}", new { Cursor = response.Cursors.After, Timestamp = timestamp });
            return timestamp;
        }

        var mostRecentTrack = items.OrderByDescending(i => DateTime.Parse(i.PlayedAt)).First();
        var fallbackTimestamp = DateTime.Parse(mostRecentTrack.PlayedAt);
        logger.LogInformation("No cursor in response, using most recent track timestamp. Context: {@Context}", new { Timestamp = fallbackTimestamp });
        return fallbackTimestamp;
    }

    private async Task<List<ListeningHistory>> BuildNewListeningHistories(SpotifyUser user, List<SpotifyRecentlyPlayedItem> items)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var candidates = items
            .Select(item => new ListeningHistory
            {
                SpotifyUserId = user.Id,
                TrackId = item.Track.Id,
                TrackName = item.Track.Name,
                Artists = item.Track.Artists.Select(a => new Artist { Id = a.Id, Name = a.Name }).ToList(),
                AlbumName = item.Track.Album.Name,
                DurationMs = item.Track.DurationMs,
                PlayedAt = DateTimeOffset.Parse(item.PlayedAt, CultureInfo.InvariantCulture).UtcDateTime,
                ContextType = item.Context?.Type,
                ContextUri = item.Context?.Uri,
            })
            .ToList();

        var minPlayedAt = candidates.Min(c => c.PlayedAt);
        var maxPlayedAt = candidates.Max(c => c.PlayedAt);
        var trackIds = candidates.Select(c => c.TrackId).Distinct().ToList();

        var existingKeys = await dbContext
            .ListeningHistories.Where(lh =>
                lh.SpotifyUserId == user.Id && lh.PlayedAt >= minPlayedAt && lh.PlayedAt <= maxPlayedAt && trackIds.Contains(lh.TrackId)
            )
            .Select(lh => new { lh.TrackId, lh.PlayedAt })
            .ToListAsync();
        var existingSet = new HashSet<(string TrackId, DateTime PlayedAt)>(existingKeys.Select(key => (key.TrackId, key.PlayedAt)));
        return candidates.Where(c => !existingSet.Contains((c.TrackId, c.PlayedAt))).ToList();
    }
}
