using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Listenfy.Application.Jobs;

public class FetchListeningDataJob(
    ApplicationDbContext dbContext,
    ISpotifyService spotifyService,
    TimeProvider timeProvider,
    ILogger<FetchListeningDataJob> logger
)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting FetchListeningDataJob");

        var spotifyUsers = await dbContext.SpotifyUsers.Include(u => u.SpotifyFetchMetadata).ToListAsync();
        logger.LogInformation("Found {Count} Spotify users to process", spotifyUsers.Count);

        foreach (var user in spotifyUsers)
        {
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
        logger.LogDebug("Processing user {SpotifyUserId}", user.SpotifyUserId);

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
            logger.LogDebug("No tracks available for first-time fetch for user {SpotifyUserId}", user.SpotifyUserId);

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
            ArtistName = string.Join(", ", item.Track.Artists.Select(a => a.Name)),
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
        logger.LogDebug("Fetching tracks for user {SpotifyUserId} after timestamp {After}", user.SpotifyUserId, afterTimestampMilliseconds);

        var result = await spotifyService.GetRecentlyPlayedTracks(user, afterTimestampMilliseconds);
        if (result.IsFailure)
        {
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
            logger.LogDebug("No new tracks for user {SpotifyUserId}", user.SpotifyUserId);
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
            ArtistName = string.Join(", ", item.Track.Artists.Select(a => a.Name)),
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

    private DateTime GetLastFetchedTimestamp(SpotifyRecentlyPlayedTracksResponse response, List<SpotifyRecentlyPlayedItem> items)
    {
        if (!string.IsNullOrEmpty(response.Cursors.After))
        {
            var cursorTimestamp = long.Parse(response.Cursors.After);
            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cursorTimestamp).UtcDateTime;
            logger.LogDebug("Using cursor timestamp {Timestamp} ({DateTime})", response.Cursors.After, timestamp);
            return timestamp;
        }

        var mostRecentTrack = items.OrderByDescending(i => DateTime.Parse(i.PlayedAt)).First();
        var fallbackTimestamp = DateTime.Parse(mostRecentTrack.PlayedAt);
        logger.LogDebug("No cursor in response, using most recent track timestamp: {DateTime}", fallbackTimestamp);
        return fallbackTimestamp;
    }
}
