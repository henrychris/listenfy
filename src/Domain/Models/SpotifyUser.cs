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
    public ICollection<UserConnection> UserConnections { get; set; } = [];
}
