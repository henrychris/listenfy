using FluentValidation;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared.Errors;
using Listenfy.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Listenfy.Application.Features.Spotify;

public class OAuthCallbackRequest : IRequest<Result<OAuthResponse>>
{
    public required string Code { get; set; }
    public required string State { get; set; }
    public string? Error { get; set; }
    public required string RedirectUri { get; set; }
}

public class OAuthResponse
{
    public required string Message { get; set; }
    public required string SpotifyUser { get; set; }
    public required ulong DiscordGuildId { get; set; }
}

public class Handler(ApplicationDbContext dbContext, ISpotifyService spotifyService, TimeProvider timeProvider, ILogger<Handler> logger)
    : IRequestHandler<OAuthCallbackRequest, Result<OAuthResponse>>
{
    public async Task<Result<OAuthResponse>> Handle(OAuthCallbackRequest request, CancellationToken cancellationToken)
    {
        if (request.Error is not null)
        {
            var connectionToDelete = await dbContext
                .UserConnections.Include(u => u.Guild)
                .FirstOrDefaultAsync(u => u.OAuthState == request.State && u.SpotifyUserId == null, cancellationToken: cancellationToken);
            if (connectionToDelete is not null)
            {
                logger.LogWarning("User denied Spotify OAuth access. Error: {Error}, State: {State}", request.Error, request.State);
                dbContext.UserConnections.Remove(connectionToDelete);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return Result<OAuthResponse>.Failure(Errors.Spotify.AuthDenied);
        }

        var userConnection = await dbContext
            .UserConnections.Include(u => u.Guild)
            .FirstOrDefaultAsync(u => u.OAuthState == request.State && u.SpotifyUserId == null);
        if (userConnection is null)
        {
            logger.LogWarning("No pending UserConnection found for state: {State}", request.State);
            return Result<OAuthResponse>.Failure(Errors.Spotify.AuthTimedOut);
        }

        var tokenResult = await spotifyService.ExchangeCodeForTokens(request.Code, request.RedirectUri);
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

public class Validator : AbstractValidator<OAuthCallbackRequest>
{
    public Validator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
    }
}
