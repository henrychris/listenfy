using Listenfy.Domain.Models;
using Refit;

namespace Listenfy.Application.Interfaces.Spotify;

public class ListeningStats
{
    public ulong DiscordUserId { get; set; }
    public string DiscordUsername { get; set; } = string.Empty;
    public List<TopTrack> TopTracks { get; set; } = [];
    public List<TopArtist> TopArtists { get; set; } = [];
    public int TotalMinutesListened { get; set; }
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

public class SpotifyAccessTokenRequest
{
    [AliasAs("grant_type")]
    public required string GrantType { get; set; }

    [AliasAs("code")]
    public required string Code { get; set; }

    [AliasAs("redirect_uri")]
    public required string RedirectUri { get; set; }
}

public class SpotifyRefreshTokenRequest
{
    [AliasAs("grant_type")]
    public required string GrantType { get; set; }

    [AliasAs("refresh_token")]
    public required string RefreshToken { get; set; }

    [AliasAs("client_id")]
    public required string ClientId { get; set; }
}

#region RECENTLY PLAYED TRACKS

public class SpotifyRecentlyPlayedTracksRequest
{
    [AliasAs("limit")]
    public int Limit { get; set; }

    [AliasAs("after")]
    public int? After { get; set; }

    [AliasAs("before")]
    public int? Before { get; set; }
}

public class SpotifyRecentlyPlayedTracksResponse
{
    public string Href { get; set; } = string.Empty;
    public int Limit { get; set; }
    public string? Next { get; set; }
    public SpotifyRecentlyPlayedCursor Cursors { get; set; } = new();
    public int Total { get; set; }
    public List<SpotifyRecentlyPlayedItem> Items { get; set; } = [];
}

public class SpotifyRecentlyPlayedCursor
{
    public string After { get; set; } = string.Empty;
    public string Before { get; set; } = string.Empty;
}

public class SpotifyRecentlyPlayedItem
{
    public SpotifyTrack Track { get; set; } = new();
    public string PlayedAt { get; set; } = string.Empty;
    public SpotifyContext? Context { get; set; }
}

public class SpotifyTrack
{
    public SpotifyAlbum Album { get; set; } = new();
    public List<SpotifyArtist> Artists { get; set; } = [];
    public List<string> AvailableMarkets { get; set; } = [];
    public int DiscNumber { get; set; }
    public int DurationMs { get; set; }
    public bool Explicit { get; set; }
    public SpotifyExternalIds ExternalIds { get; set; } = new();
    public SpotifyExternalUrls ExternalUrls { get; set; } = new();
    public string Href { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public bool IsPlayable { get; set; }
    public object? LinkedFrom { get; set; }
    public SpotifyRestrictions? Restrictions { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Popularity { get; set; }
    public string? PreviewUrl { get; set; }
    public int TrackNumber { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public bool IsLocal { get; set; }
}

public class SpotifyAlbum
{
    public string AlbumType { get; set; } = string.Empty;
    public int TotalTracks { get; set; }
    public List<string> AvailableMarkets { get; set; } = [];
    public SpotifyExternalUrls ExternalUrls { get; set; } = new();
    public string Href { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public List<SpotifyImage> Images { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string ReleaseDatePrecision { get; set; } = string.Empty;
    public SpotifyRestrictions? Restrictions { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public List<SpotifyArtist> Artists { get; set; } = [];
}

public class SpotifyArtist
{
    public SpotifyExternalUrls ExternalUrls { get; set; } = new();
    public string Href { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
}

public class SpotifyImage
{
    public string Url { get; set; } = string.Empty;
    public int Height { get; set; }
    public int Width { get; set; }
}

public class SpotifyExternalUrls
{
    public string Spotify { get; set; } = string.Empty;
}

public class SpotifyExternalIds
{
    public string? Isrc { get; set; }
    public string? Ean { get; set; }
    public string? Upc { get; set; }
}

public class SpotifyRestrictions
{
    public string Reason { get; set; } = string.Empty;
}

public class SpotifyContext
{
    public string Type { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public SpotifyExternalUrls ExternalUrls { get; set; } = new();
    public string Uri { get; set; } = string.Empty;
}

#endregion
