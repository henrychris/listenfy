using Refit;

namespace Listenfy.Application.Interfaces.Spotify;

public interface ISpotifyAccountApi
{
    [Post("/api/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<ApiResponse<SpotifyTokenResponse>> RequestAccessToken(
        [Body(BodySerializationMethod.UrlEncoded)] SpotifyAccessTokenRequest request,
        [Authorize("Basic")] string authorization
    );

    [Post("/api/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<ApiResponse<SpotifyTokenResponse>> RequestPKCEAccessToken([Body(BodySerializationMethod.UrlEncoded)] SpotifyPKCEAccessTokenRequest request);

    [Post("/api/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<ApiResponse<SpotifyTokenResponse>> RefreshAccessToken(
        [Body(BodySerializationMethod.UrlEncoded)] SpotifyRefreshTokenRequest request,
        [Authorize("Basic")] string? authorization = null
    );

    [Post("/api/token")]
    [Headers("Content-Type: application/x-www-form-urlencoded")]
    Task<ApiResponse<SpotifyTokenResponse>> RefreshAccessTokenPKCE([Body(BodySerializationMethod.UrlEncoded)] SpotifyRefreshTokenRequest request);
}
