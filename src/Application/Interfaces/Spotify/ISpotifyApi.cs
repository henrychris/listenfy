using Refit;

namespace Listenfy.Application.Interfaces.Spotify;

public interface ISpotifyApi
{
    [Get("/me")]
    Task<ApiResponse<SpotifyProfile>> GetUserProfile([Authorize] string authorization);
}
