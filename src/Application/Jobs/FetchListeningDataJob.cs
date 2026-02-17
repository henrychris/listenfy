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
        logger.LogInformation("Found {Count} Spotify users to process", spotifyUsers.Count);

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
                logger.LogError(ex, "Error processing listening history for SpotifyUser {SpotifyUserId}", user.SpotifyUserId);
            }
        }

        logger.LogInformation("Completed FetchListeningDataJob");
    }

    private async Task ProcessUserListeningHistory(SpotifyUser user)
    {
        logger.LogInformation("Processing user {SpotifyUserId}", user.SpotifyUserId);

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
        logger.LogInformation("First-time fetch for user {SpotifyUserId}", user.SpotifyUserId);

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
                "Failed to fetch recently played tracks for user {SpotifyUserId}. Error: {Error}",
                user.SpotifyUserId,
                result.Error.Description
            );
            return;
        }

        var response = result.Value;
        var items = response.Items;
        if (items.Count == 0)
        {
            logger.LogInformation("No tracks available for first-time fetch for user {SpotifyUserId}", user.SpotifyUserId);

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

        logger.LogInformation("Found {Count} tracks for first-time fetch for user {SpotifyUserId}", items.Count, user.SpotifyUserId);
        var listeningHistories = items.Select(item => new ListeningHistory
        {
            SpotifyUserId = user.Id,
            TrackId = item.Track.Id,
            TrackName = item.Track.Name,
            Artists = item.Track.Artists.Select(a => new Artist { Id = a.Id, Name = a.Name }).ToList(),
            AlbumName = item.Track.Album.Name,
            DurationMs = item.Track.DurationMs,
            PlayedAt = DateTime.Parse(item.PlayedAt),
            ContextType = item.Context?.Type,
            ContextUri = item.Context?.Uri,
        });
        await dbContext.ListeningHistories.AddRangeAsync(listeningHistories);

        var newMetadata = new SpotifyFetchMetadata
        {
            SpotifyUserId = user.Id,
            LastFetchedAt = GetLastFetchedTimestamp(response, items),
            TracksFetchedInLastRun = items.Count,
        };
        dbContext.SpotifyFetchMetadata.Add(newMetadata);

        await dbContext.SaveChangesAsync();
        logger.LogInformation(
            "Saved {Count} tracks for user {SpotifyUserId}. Last fetched at: {LastFetchedAt}",
            items.Count,
            user.SpotifyUserId,
            newMetadata.LastFetchedAt
        );
    }

    private async Task ProcessSubsequentFetch(SpotifyUser user, SpotifyFetchMetadata metadata)
    {
        var afterTimestampMilliseconds = new DateTimeOffset(metadata.LastFetchedAt).ToUnixTimeMilliseconds();
        logger.LogInformation(
            "Fetching tracks for user {SpotifyUserId} after timestamp {After}. Date: {Date}",
            user.SpotifyUserId,
            afterTimestampMilliseconds,
            metadata.LastFetchedAt
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
                "Failed to fetch recently played tracks for user {SpotifyUserId}. Error: {Error}",
                user.SpotifyUserId,
                result.Error.Description
            );
            return;
        }

        var response = result.Value;
        var items = response.Items;
        if (items.Count == 0)
        {
            logger.LogInformation("No new tracks for user {SpotifyUserId}", user.SpotifyUserId);
            // Don't update LastFetchedAt if there are no new tracks - keep using the same cursor
            metadata.TracksFetchedInLastRun = 0;
            await dbContext.SaveChangesAsync();
            return;
        }

        logger.LogInformation("Found {Count} new tracks for user {SpotifyUserId}", items.Count, user.SpotifyUserId);

        // Save tracks to listening history
        var listeningHistories = items.Select(item => new ListeningHistory
        {
            SpotifyUserId = user.Id,
            TrackId = item.Track.Id,
            TrackName = item.Track.Name,
            Artists = item.Track.Artists.Select(a => new Artist { Id = a.Id, Name = a.Name }).ToList(),
            AlbumName = item.Track.Album.Name,
            DurationMs = item.Track.DurationMs,
            PlayedAt = DateTime.Parse(item.PlayedAt), // Spotify returns ISO 8601
            ContextType = item.Context?.Type,
            ContextUri = item.Context?.Uri,
        });

        await dbContext.ListeningHistories.AddRangeAsync(listeningHistories);

        metadata.LastFetchedAt = GetLastFetchedTimestamp(response, items);
        metadata.TracksFetchedInLastRun = items.Count;
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Saved {Count} tracks for user {SpotifyUserId}. Last fetched at: {LastFetchedAt}",
            items.Count,
            user.SpotifyUserId,
            metadata.LastFetchedAt
        );
    }

    private async Task HandleExpiredRefreshToken(SpotifyUser user)
    {
        logger.LogWarning("Refresh token expired for SpotifyUser {SpotifyUserId}. Removing user connections.", user.SpotifyUserId);

        // Get all connections for this user (they may be connected to multiple guilds)
        var connections = user.UserConnections.ToList();
        foreach (var connection in connections)
        {
            var guildName = connection.Guild?.GuildName ?? "Unknown Server";
            logger.LogInformation(
                "Notifying Discord user {DiscordUserId} about expired token in guild {GuildName} ({GuildId})",
                connection.DiscordUserId,
                guildName,
                connection.Guild?.DiscordGuildId ?? 0
            );

            // Notify the user via DM
            await notificationService.NotifyRefreshTokenExpiredAsync(connection.DiscordUserId, guildName);
        }

        // Remove all connections for this user
        dbContext.UserConnections.RemoveRange(connections);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Removed SpotifyUser {SpotifyUserId} and associated connections due to expired token", user.SpotifyUserId);
    }

    private DateTime GetLastFetchedTimestamp(SpotifyRecentlyPlayedTracksResponse response, List<SpotifyRecentlyPlayedItem> items)
    {
        if (!string.IsNullOrEmpty(response.Cursors.After))
        {
            var cursorTimestamp = long.Parse(response.Cursors.After);
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cursorTimestamp).UtcDateTime;
            logger.LogInformation("Using cursor timestamp {Timestamp} ({DateTime})", response.Cursors.After, timestamp);
            return timestamp;
        }

        var mostRecentTrack = items.OrderByDescending(i => DateTime.Parse(i.PlayedAt)).First();
        var fallbackTimestamp = DateTime.Parse(mostRecentTrack.PlayedAt);
        logger.LogInformation("No cursor in response, using most recent track timestamp: {DateTime}", fallbackTimestamp);
        return fallbackTimestamp;
    }
}
