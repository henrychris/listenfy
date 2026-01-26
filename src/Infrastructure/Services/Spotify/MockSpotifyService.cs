using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Domain.Models;

namespace Listenfy.Infrastructure.Services.Spotify;

public class MockSpotifyService : ISpotifyService
{
    public Task<bool> CompleteAuthorization(string code, ulong discordUserId, ulong guildId)
    {
        throw new NotImplementedException();
    }

    public Task<SpotifyTokenResponse> ExchangeCodeForTokens(string code, string redirectUri)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetAuthorizationUrl(ulong discordUserId)
    {
        throw new NotImplementedException();
    }

    public Task<SpotifyProfile> GetCurrentUserProfile(string accessToken)
    {
        throw new NotImplementedException();
    }

    public Task<ListeningStats> GetUserStats(ulong discordUserId, TimeSpan period)
    {
        throw new NotImplementedException();
    }

    public Task<SpotifyTokenResponse> RefreshAccessToken(string refreshToken)
    {
        throw new NotImplementedException();
    }

    public Task RefreshTokenIfNeeded(UserConnection connection)
    {
        throw new NotImplementedException();
    }
}
