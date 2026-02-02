using System.Text;
using System.Web;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Application.Settings;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared.Errors;
using Listenfy.Shared.Results;
using Microsoft.Extensions.Options;

namespace Listenfy.Infrastructure.Services.Spotify;

public class SpotifyService(
    IOptions<SpotifySettings> options,
    ISpotifyAccountApi spotifyAccountApi,
    ISpotifyApi spotifyApi,
    ApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<SpotifyService> logger
) : ISpotifyService
{
    private readonly SpotifySettings _spotifySettings = options.Value;

    /// <summary>
    /// Gets the authorization URL for Spotify authentication.
    /// </summary>
    /// <param name="oAuthState">used to identify the user connection model in DB when auth is completed</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetAuthorizationUrl(string oAuthState)
    {
        var scopes = "user-read-recently-played user-top-read user-read-private user-read-email";
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams["client_id"] = _spotifySettings.ClientId;
        queryParams["redirect_uri"] = _spotifySettings.RedirectUrl;
        queryParams["state"] = oAuthState;
        queryParams["response_type"] = "code";
        queryParams["scope"] = scopes;

        return $"{_spotifySettings.AccountsBaseUrl}/authorize?{queryParams}";
    }

    public async Task<Result<SpotifyTokenResponse>> ExchangeCodeForTokens(string code, string redirectUri)
    {
        var spotifyCredentials = $"{_spotifySettings.ClientId}:{_spotifySettings.ClientSecret}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(spotifyCredentials));

        var response = await spotifyAccountApi.RequestAccessToken(
            new SpotifyAccessTokenRequest
            {
                GrantType = "authorization_code",
                Code = code,
                RedirectUri = redirectUri,
            },
            token
        );
        if (!response.IsSuccessful)
        {
            logger.LogError("Failed to exchange code for tokens. Error: {ex}", response.Error);
            return Result<SpotifyTokenResponse>.Failure(Errors.Spotify.TokenExchangeFailed);
        }

        return Result<SpotifyTokenResponse>.Success(response.Content);
    }

    public async Task<Result<SpotifyProfile>> GetCurrentUserProfile(string accessToken)
    {
        var response = await spotifyApi.GetUserProfile(accessToken);
        if (!response.IsSuccessful)
        {
            logger.LogError("Failed to get user profile. Error: {ex}", response.Error);
            return Result<SpotifyProfile>.Failure(Errors.Spotify.ProfileFetchFailed);
        }

        return Result<SpotifyProfile>.Success(response.Content);
    }

    public Task<Result<ListeningStats>> GetUserStats(ulong discordUserId, TimeSpan period)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<SpotifyTokenResponse>> RefreshAccessToken(string refreshToken)
    {
        var response = await spotifyAccountApi.RefreshAccessToken(
            new SpotifyRefreshTokenRequest
            {
                GrantType = "refresh_token",
                RefreshToken = refreshToken,
                ClientId = _spotifySettings.ClientId,
            }
        );
        if (!response.IsSuccessful)
        {
            // todo: if this happens, do we clear the user and force a re-login?
            logger.LogError("Failed to refresh access token. Error: {ex}", response.Error);
            return Result<SpotifyTokenResponse>.Failure(Errors.Spotify.TokenRefreshFailed);
        }

        return Result<SpotifyTokenResponse>.Success(response.Content);
    }

    // <summary>
    /// Refreshes the access token if it's expired.
    /// </summary>
    /// <param name="spotifyUser"></param>
    /// <returns>Returns the access token if it's still valid, otherwise refreshes it and returns the new token.</returns>
    public async Task<Result<string>> RefreshTokenIfNeeded(SpotifyUser spotifyUser)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (spotifyUser.TokenExpiresAt > now.AddMinutes(1))
        {
            logger.LogDebug("Access token is still valid for another minute. Returning as is.");
            return Result<string>.Success(spotifyUser.AccessToken);
        }

        var refreshResult = await RefreshAccessToken(spotifyUser.RefreshToken);
        if (refreshResult.IsFailure)
        {
            return Result<string>.Failure(refreshResult.Error);
        }

        var tokenResponse = refreshResult.Value;
        spotifyUser.AccessToken = tokenResponse.AccessToken;
        if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            spotifyUser.RefreshToken = tokenResponse.RefreshToken;
        }

        spotifyUser.TokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresIn);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Access token refreshed for spotify user: {SpotifyUserId}", spotifyUser.Id);
        return Result<string>.Success(spotifyUser.AccessToken);
    }

    }
}
