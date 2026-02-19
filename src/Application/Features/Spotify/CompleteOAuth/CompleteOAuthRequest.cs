using FluentValidation;
using Hangfire;
using Listenfy.Application.Features.Spotify.Shared;
using Listenfy.Application.Interfaces;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Application.Jobs;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared.Errors;
using Listenfy.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Listenfy.Application.Features.Spotify.CompleteOAuth;

public class CompleteOAuthRequest : IRequest<Result<OAuthResponse>>
{
    public string Code { get; set; } = null!;
    public string State { get; set; } = null!;
    public string CodeVerifier { get; set; } = null!;
    public string ClientId { get; set; } = null!;
}

public class Handler(
    ApplicationDbContext dbContext,
    ISpotifyService spotifyService,
    INotificationService notificationService,
    TimeProvider timeProvider,
    IBackgroundJobClient backgroundJobClient,
    ILogger<Handler> logger
) : IRequestHandler<CompleteOAuthRequest, Result<OAuthResponse>>
{
    public async Task<Result<OAuthResponse>> Handle(CompleteOAuthRequest request, CancellationToken cancellationToken)
    {
        var userConnection = await dbContext
            .UserConnections.Include(u => u.Guild)
            .FirstOrDefaultAsync(u => u.OAuthState == request.State && u.SpotifyUserId == null, cancellationToken: cancellationToken);
        if (userConnection is null)
        {
            logger.LogWarning("No pending UserConnection found for state: {State}", request.State);
            return Result<OAuthResponse>.Failure(Errors.Spotify.AuthTimedOut);
        }

        var tokenResult = await spotifyService.ExchangeCodePKCE(request.Code, request.CodeVerifier, request.ClientId);
        if (tokenResult.IsFailure)
        {
            return Result<OAuthResponse>.Failure(tokenResult.Error);
        }
        var tokenResponse = tokenResult.Value;

        var profileRes = await spotifyService.GetCurrentUserProfile(tokenResponse.AccessToken);
        if (profileRes.IsFailure)
        {
            return Result<OAuthResponse>.Failure(profileRes.Error);
        }
        var profile = profileRes.Value;

        var spotifyUser = await dbContext.SpotifyUsers.FirstOrDefaultAsync(s => s.SpotifyUserId == profile.Id, cancellationToken: cancellationToken);
        if (spotifyUser is null)
        {
            spotifyUser = new()
            {
                SpotifyUserId = profile.Id,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenExpiresAt = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(tokenResponse.ExpiresIn),
            };

            dbContext.SpotifyUsers.Add(spotifyUser);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Created new SpotifyUser: {SpotifyId}", profile.Id);
        }
        else
        {
            spotifyUser.AccessToken = tokenResponse.AccessToken;
            spotifyUser.RefreshToken = tokenResponse.RefreshToken;
            spotifyUser.TokenExpiresAt = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(tokenResponse.ExpiresIn);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated existing SpotifyUser tokens: {SpotifyId}", profile.Id);
        }

        userConnection.SpotifyUserId = spotifyUser.Id;
        userConnection.OAuthState = null;
        userConnection.ConnectedAt = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Completed OAuth flow for Discord user {DiscordUserId} in guild {GuildId}",
            userConnection.DiscordUserId,
            userConnection.Guild.DiscordGuildId
        );

        // Notify the user via Discord DM that connection was successful
        backgroundJobClient.Enqueue(
            () => notificationService.NotifyConnectionSuccessAsync(userConnection.DiscordUserId, userConnection.Guild.GuildName)
        );

        // immediately fetch listening data so stats are available faster, instead of waiting for the next scheduled job run
        backgroundJobClient.Enqueue<FetchListeningDataJob>(job => job.ExecuteForUserAsync(spotifyUser.Id));
        return Result<OAuthResponse>.Success(
            new OAuthResponse
            {
                Message = "✅ Successfully connected to Spotify!",
                SpotifyUser = profile.DisplayName,
                DiscordGuildId = userConnection.Guild.DiscordGuildId,
            }
        );
    }
}

public class Validator : AbstractValidator<CompleteOAuthRequest>
{
    public Validator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.CodeVerifier).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty();
    }
}
