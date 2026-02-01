using System.Text;
using System.Web;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Application.Settings;
using Listenfy.Domain.Models;
using Listenfy.Shared.Errors;
using Listenfy.Shared.Results;
using Microsoft.Extensions.Options;

namespace Listenfy.Infrastructure.Services.Spotify;

public class SpotifyService(
    IOptions<SpotifySettings> options,
    ISpotifyAccountApi spotifyAccountApi,
    ISpotifyApi spotifyApi,
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

    public Task<Result<bool>> CompleteAuthorization(string code, ulong discordUserId, ulong guildId)
    {
        throw new NotImplementedException();
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

    public Task<Result<SpotifyTokenResponse>> RefreshAccessToken(string refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task<Result<MyUnit>> RefreshTokenIfNeeded(UserConnection connection)
    {
        throw new NotImplementedException();
    }
}
