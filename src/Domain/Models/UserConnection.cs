namespace Listenfy.Domain.Models;

/// <summary>
/// Represents a Discord user's connection in a specific server
/// </summary>
public class UserConnection : BaseEntity
{
    public ulong DiscordUserId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public string? OAuthState { get; set; } // State token for OAuth verification (null after completion)

    // Guild relationship (Discord user in a specific server)
    public required string GuildId { get; set; }
    public GuildSettings Guild { get; set; } = null!;

    // Spotify relationship
    public string? SpotifyUserId { get; set; } // Foreign key to SpotifyUser
    public SpotifyUser? SpotifyUser { get; set; }
}
