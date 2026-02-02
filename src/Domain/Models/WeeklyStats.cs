namespace Listenfy.Domain.Models;

public class WeeklyStat : BaseEntity
{
    // Week identifier (e.g., "2025-W05" for ISO week)
    public required string WeekIdentifier { get; set; }
    public required DateTime WeekStartDate { get; set; }
    public required DateTime WeekEndDate { get; set; }

    // Precomputed data (stored as JSON)
    public required List<TopTrack> TopTracks { get; set; }
    public required List<TopArtist> TopArtists { get; set; }

    public int TotalMinutesListened { get; set; }
    public int TotalTracksPlayed { get; set; }
    public int UniqueTracksPlayed { get; set; }

    public DateTime ComputedAt { get; set; }

    public required string SpotifyUserId { get; set; }
    public SpotifyUser SpotifyUser { get; set; } = null!;
}

public class TopTrack
{
    public string Name { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int PlayCount { get; set; }
}

public class TopArtist
{
    public string Name { get; set; } = string.Empty;
    public int PlayCount { get; set; }
}
