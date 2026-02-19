namespace Listenfy.Application.Features.Spotify.Shared;

public class OAuthResponse
{
    public required string Message { get; set; }
    public required string SpotifyUser { get; set; }
    public required ulong DiscordGuildId { get; set; }
}
