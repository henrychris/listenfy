using Listenfy.Domain.Models;
using Listenfy.Shared.Results;

namespace Listenfy.Application.Interfaces.Spotify;

public interface ISpotifyService
{
    string GetAuthorizationUrl(string oAuthState);

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens
    /// </summary>
    Task<Result<SpotifyTokenResponse>> ExchangeCodeForTokens(string code);

    /// <summary>
    /// Gets the current user's Spotify profile information
    /// </summary>
    Task<Result<SpotifyProfile>> GetCurrentUserProfile(string accessToken);

    /// <summary>
    /// Refreshes an expired access token using the refresh token
    /// </summary>
    Task<Result<SpotifyTokenResponse>> RefreshAccessToken(SpotifyUser spotifyUser);
    Task<Result<string>> RefreshTokenIfNeeded(SpotifyUser spotifyUser);
    Task<Result<SpotifyRecentlyPlayedTracksResponse>> GetRecentlyPlayedTracks(SpotifyUser spotifyUser, long? afterInMilliseconds = null);
    Task<Result<SpotifyTokenResponse>> ExchangeCodePKCE(string code, string codeVerifier, string clientId);
}
