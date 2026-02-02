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

        // Get or create fetch metadata
        var metadata = user.SpotifyFetchMetadata;
        if (metadata is null)
        {
            metadata = new SpotifyFetchMetadata
            {
                SpotifyUserId = user.Id,
                LastFetchedAt = DateTime.MinValue,
                TracksFetchedInLastRun = 0,
            };

            dbContext.SpotifyFetchMetadata.Add(metadata);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Created new SpotifyFetchMetadata for user {SpotifyUserId}", user.SpotifyUserId);
        }

        // Calculate the 'after' timestamp (in Unix milliseconds)
        int? afterTimestamp = null;
        if (metadata.LastPlayedAt.HasValue)
        {
            // Convert to Unix milliseconds
            afterTimestamp = (int)new DateTimeOffset(metadata.LastPlayedAt.Value).ToUnixTimeMilliseconds();
        }

        logger.LogDebug("Fetching tracks for user {SpotifyUserId} after timestamp {After}", user.SpotifyUserId, afterTimestamp);

        var result = await spotifyService.GetRecentlyPlayedTracks(user, afterTimestamp);
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
            metadata.LastFetchedAt = timeProvider.GetUtcNow().UtcDateTime;
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

        // Update metadata
        var mostRecentTrack = items.MaxBy(i => DateTime.Parse(i.PlayedAt));
        if (mostRecentTrack != null)
        {
            metadata.LastPlayedAt = DateTime.Parse(mostRecentTrack.PlayedAt);
        }

        metadata.LastFetchedAt = timeProvider.GetUtcNow().UtcDateTime;
        metadata.TracksFetchedInLastRun = items.Count;
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Saved {Count} tracks for user {SpotifyUserId}. Last played at: {LastPlayedAt}",
            items.Count,
            user.SpotifyUserId,
            metadata.LastPlayedAt
        );
    }
}
