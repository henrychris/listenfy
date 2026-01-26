using Listenfy.Domain.Models;

namespace Listenfy.Application.Interfaces.Spotify;

public interface ISpotifyService
{
    Task<string> GetAuthorizationUrl(ulong discordUserId);

    /// <summary>
    /// Exchanges an authorization code for access and refresh tokens
    /// </summary>
    Task<SpotifyTokenResponse> ExchangeCodeForTokens(string code, string redirectUri);

    /// <summary>
    /// Gets the current user's Spotify profile information
    /// </summary>
    Task<SpotifyProfile> GetCurrentUserProfile(string accessToken);

    /// <summary>
    /// Refreshes an expired access token using the refresh token
    /// </summary>
    Task<SpotifyTokenResponse> RefreshAccessToken(string refreshToken);

    Task<bool> CompleteAuthorization(string code, ulong discordUserId, ulong guildId);
    Task<ListeningStats> GetUserStats(ulong discordUserId, TimeSpan period);
    Task RefreshTokenIfNeeded(UserConnection connection);
}
