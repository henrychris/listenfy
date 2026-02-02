namespace Listenfy.Domain.Models;

/// <summary>
/// Represents a unique Spotify account
/// </summary>
public class SpotifyUser : BaseEntity
{
    public required string SpotifyUserId { get; set; } // Spotify's ID, unique
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime TokenExpiresAt { get; set; }

    // Navigation property
    public SpotifyFetchMetadata? SpotifyFetchMetadata { get; set; }
    public ICollection<ListeningHistory> ListeningHistories { get; set; } = [];
    public ICollection<WeeklyStat> WeeklyStats { get; set; } = [];
    public ICollection<UserConnection> UserConnections { get; set; } = [];
}
