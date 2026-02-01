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
}
