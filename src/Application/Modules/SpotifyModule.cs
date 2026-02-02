using Listenfy.Application.Interfaces;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Microsoft.EntityFrameworkCore;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Listenfy.Application.Modules;

public class SpotifyModule(
    ApplicationDbContext dbContext,
    IStatsService statsService,
    ISpotifyService spotifyService,
    TimeProvider timeProvider,
    ILogger<SpotifyModule> logger
) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("connect", "Connect your Spotify account in this server")]
    public async Task ConnectAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue || Context.Guild is null)
        {
            logger.LogWarning("Connect command invoked outside of a server");
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Connect command started. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);

        // Check if user already has a COMPLETE connection
        var existingUserConnection = await dbContext
            .UserConnections.Include(u => u.SpotifyUser)
            .FirstOrDefaultAsync(u => u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId);
        if (existingUserConnection?.SpotifyUser is not null)
        {
            logger.LogInformation("User already has a complete Spotify connection. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);
            await RespondAsync(InteractionCallback.Message("✅ You're already connected! Use `/disconnect` to unlink your account."));
            return;
        }

        // If incomplete connection exists, delete it so we can start fresh
        if (existingUserConnection is not null)
        {
            logger.LogInformation(
                "Incomplete connection found, deleting. GuildId: {GuildId}, UserId: {UserId}, ConnectionId: {ConnectionId}",
                guildId.Value,
                userId,
                existingUserConnection.Id
            );
            dbContext.UserConnections.Remove(existingUserConnection);
            await dbContext.SaveChangesAsync();
        }

        // Ensure guild settings exist
        var guildSettings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
        if (guildSettings is null)
        {
            logger.LogInformation("Creating new GuildSettings during Connect. GuildId: {GuildId}", guildId.Value);
            guildSettings = new GuildSettings { DiscordGuildId = guildId.Value };
            dbContext.GuildSettings.Add(guildSettings);
        }

        // Create pending connection
        var oAuthState = Guid.NewGuid().ToString("N");
        var connection = new UserConnection
        {
            GuildId = guildSettings.Id,
            DiscordUserId = userId,
            ConnectedAt = timeProvider.GetUtcNow().UtcDateTime,
            OAuthState = oAuthState,
        };

        dbContext.UserConnections.Add(connection);
        await dbContext.SaveChangesAsync();
        logger.LogInformation(
            "Created pending UserConnection. GuildId: {GuildId}, UserId: {UserId}, ConnectionId: {ConnectionId}, OAuthState: {OAuthState}",
            guildId.Value,
            userId,
            connection.Id,
            oAuthState
        );

        var authUrl = spotifyService.GetAuthorizationUrl(oAuthState);
        logger.LogDebug("Generated authorization URL for UserId: {UserId}. URL: {AuthUrl}", userId, authUrl);

        await RespondAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties
                {
                    Embeds =
                    [
                        new EmbedProperties
                        {
                            Url = authUrl,
                            Title = $"Click here to connect your Spotify Account.\n\n_Note: This connection is for **{Context.Guild.Name}** only._",
                            Thumbnail = new EmbedThumbnailProperties(
                                "https://storage.googleapis.com/pr-newsroom-wp/1/2023/05/Spotify_Full_Logo_RGB_Green.png"
                            ),
                        },
                    ],
                }
            )
        );
    }

    [SlashCommand("disconnect", "Disconnect your Spotify account in this server")]
    public async Task DisconnectAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue)
        {
            logger.LogWarning("Disconnect command invoked outside of a server");
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Disconnect command started. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);

        var connection = await dbContext.UserConnections.FirstOrDefaultAsync(u =>
            u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId
        );
        if (connection is null)
        {
            logger.LogInformation("No Spotify connection found to disconnect. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);
            await RespondAsync(InteractionCallback.Message("❌ You don't have a connected Spotify account in this server."));
            return;
        }

        logger.LogInformation(
            "Disconnecting Spotify account. GuildId: {GuildId}, UserId: {UserId}, ConnectionId: {ConnectionId}",
            guildId.Value,
            userId,
            connection.Id
        );
        dbContext.UserConnections.Remove(connection);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Spotify account successfully disconnected. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);
        await RespondAsync(InteractionCallback.Message("✅ Your Spotify account has been disconnected from this server."));
    }

    [SlashCommand("stats", "View your Spotify listening stats")]
    public async Task StatsAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue)
        {
            logger.LogWarning("Stats command invoked outside of a server");
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Stats command started. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);

        var connection = await dbContext
            .UserConnections.Include(u => u.SpotifyUser)
            .FirstOrDefaultAsync(u => u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId);
        if (connection is null || connection.SpotifyUser is null)
        {
            logger.LogInformation(
                "No Spotify connection found for stats. GuildId: {GuildId}, UserId: {UserId}, ConnectionExists: {ConnectionExists}, SpotifyUserExists: {SpotifyUserExists}",
                guildId.Value,
                userId,
                connection is not null,
                connection?.SpotifyUser is not null
            );
            await RespondAsync(InteractionCallback.Message("❌ You haven't connected your Spotify yet! Use `/connect` first."));
            return;
        }

        logger.LogDebug(
            "Found Spotify connection. GuildId: {GuildId}, UserId: {UserId}, ConnectionId: {ConnectionId}",
            guildId.Value,
            userId,
            connection.Id
        );
        await RespondAsync(InteractionCallback.DeferredMessage());

        try
        {
            logger.LogDebug("Refreshing tokens if needed. UserId: {UserId}", userId);
            // Refresh tokens if needed before making API calls
            await spotifyService.RefreshTokenIfNeeded(connection.SpotifyUser);

            logger.LogInformation("Generating stats. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);
            var stats = await statsService.GenerateUserStats(guildId.Value, userId);
            logger.LogInformation("Stats generated successfully. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);
            await Context.Interaction.SendFollowupMessageAsync(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching stats. GuildId: {GuildId}, UserId: {UserId}", guildId.Value, userId);
            await Context.Interaction.SendFollowupMessageAsync($"❌ Something went wrong when fetching your stats. Try again?");
        }
    }
}
