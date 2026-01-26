namespace Listenfy.Application.Interfaces.Spotify;

public class ListeningStats
{
    public ulong DiscordUserId { get; set; }
    public string DiscordUsername { get; set; } = string.Empty;
    public List<TopTrack> TopTracks { get; set; } = [];
    public List<TopArtist> TopArtists { get; set; } = [];
    public int TotalMinutesListened { get; set; }
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

public class SpotifyTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string? Scope { get; set; }
}

public class SpotifyProfile
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
