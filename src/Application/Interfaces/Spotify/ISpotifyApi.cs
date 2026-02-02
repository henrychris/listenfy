using Refit;

namespace Listenfy.Application.Interfaces.Spotify;

public interface ISpotifyApi
{
    [Get("/me")]
    Task<ApiResponse<SpotifyProfile>> GetUserProfile([Authorize] string authorization);

    [Get("/me/player/recently-played")]
    Task<ApiResponse<SpotifyRecentlyPlayedTracksResponse>> GetRecentlyPlayedTracks(
        [Authorize] string authorization,
        [Query] SpotifyRecentlyPlayedTracksRequest request
    );
}
