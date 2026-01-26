using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;

namespace Listenfy.Application.Services;

/// <summary>
/// Service for managing Spotify token refresh logic
/// </summary>
public class SpotifyTokenService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider,
    ISpotifyService spotifyService,
    ILogger<SpotifyTokenService> logger
)
{
    /// <summary>
    /// Refreshes tokens if they're expired or expiring soon (within 5 minutes)
    /// </summary>
    public async Task EnsureValidTokensAsync(UserConnection connection)
    {
        if (connection.SpotifyUser is null)
        {
            throw new InvalidOperationException("UserConnection has no linked SpotifyUser");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var tokenExpiresAt = connection.SpotifyUser.TokenExpiresAt;

        // Refresh if expired or expiring within 5 minutes
        if (tokenExpiresAt <= now.AddMinutes(5))
        {
            logger.LogInformation("Refreshing Spotify tokens for user {DiscordUserId}", connection.DiscordUserId);

            try
            {
                var tokenResponse = await spotifyService.RefreshAccessToken(connection.SpotifyUser.RefreshToken);

                // Update the SpotifyUser with new tokens
                connection.SpotifyUser.AccessToken = tokenResponse.AccessToken;
                connection.SpotifyUser.RefreshToken = tokenResponse.RefreshToken;
                connection.SpotifyUser.TokenExpiresAt = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(tokenResponse.ExpiresIn);

                await dbContext.SaveChangesAsync();
                logger.LogInformation("Successfully refreshed Spotify tokens for user {DiscordUserId}", connection.DiscordUserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh Spotify tokens for user {DiscordUserId}", connection.DiscordUserId);
                throw;
            }
        }
    }
}
