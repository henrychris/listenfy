using Listenfy.Domain.Models;
using Listenfy.Shared.Results;

namespace Listenfy.Application.Interfaces.Spotify;

public interface ISpotifyService
{
    string GetAuthorizationUrl(string oAuthState);

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens
    /// </summary>
    Task<Result<SpotifyTokenResponse>> ExchangeCodeForTokens(string code, string redirectUri);

    /// <summary>
    /// Gets the current user's Spotify profile information
    /// </summary>
    Task<Result<SpotifyProfile>> GetCurrentUserProfile(string accessToken);

    /// <summary>
    /// Refreshes an expired access token using the refresh token
    /// </summary>
    Task<Result<SpotifyTokenResponse>> RefreshAccessToken(string refreshToken);
    Task<Result<string>> RefreshTokenIfNeeded(SpotifyUser spotifyUser);
    Task<Result<SpotifyRecentlyPlayedTracksResponse>> GetRecentlyPlayedTracks(SpotifyUser spotifyUser, int? afterInMilliseconds);
}
