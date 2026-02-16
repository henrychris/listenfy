namespace Listenfy.Domain.Models;

public class ListeningHistory : BaseEntity
{
    public required string TrackId { get; set; }
    public required string TrackName { get; set; }
    public required List<Artist> Artists { get; set; }
    public required string AlbumName { get; set; }
    public int DurationMs { get; set; }

    public required DateTime PlayedAt { get; set; } // From Spotify's played_at

    // Optional: context info
    public string? ContextType { get; set; } // album, playlist, artist, etc.
    public string? ContextUri { get; set; }

    public required string SpotifyUserId { get; set; }
    public SpotifyUser SpotifyUser { get; set; } = null!;
}

public class Artist
{
    public required string Id { get; set; }
    public required string Name { get; set; }
}
