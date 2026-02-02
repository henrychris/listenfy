namespace Listenfy.Domain.Models;

public class SpotifyFetchMetadata : BaseEntity
{
    public DateTime LastFetchedAt { get; set; }
    public DateTime? LastPlayedAt { get; set; } // Most recent track timestamp
    public int TracksFetchedInLastRun { get; set; }

    public required string SpotifyUserId { get; set; }
    public SpotifyUser SpotifyUser { get; set; } = null!;
}
