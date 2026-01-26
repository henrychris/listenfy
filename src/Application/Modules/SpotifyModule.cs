using Listenfy.Application.Interfaces;
using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
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
            await BlockUsageOutsideServer();
            return;
        }

        // Check if user already has a COMPLETE connection
        var existingUserConnection = await dbContext
            .UserConnections.Include(u => u.SpotifyUser)
            .FirstOrDefaultAsync(u => u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId);
        if (existingUserConnection?.SpotifyUser is not null)
        {
            await RespondAsync(InteractionCallback.Message("✅ You're already connected! Use `/disconnect` to unlink your account."));
            return;
        }

        // If incomplete connection exists, delete it so we can start fresh
        if (existingUserConnection != null)
        {
            dbContext.UserConnections.Remove(existingUserConnection);
            await dbContext.SaveChangesAsync();
        }

        // Ensure guild settings exist
        var guildSettings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
        if (guildSettings is null)
        {
            guildSettings = new GuildSettings { DiscordGuildId = guildId.Value };
            dbContext.GuildSettings.Add(guildSettings);
        }

        // Create pending connection
        var connection = new UserConnection
        {
            GuildId = guildSettings.Id,
            DiscordUserId = userId,
            ConnectedAt = timeProvider.GetUtcNow().UtcDateTime,
            OAuthState = Guid.NewGuid().ToString("N"),
        };

        dbContext.UserConnections.Add(connection);
        await dbContext.SaveChangesAsync();

        var authUrl = await spotifyService.GetAuthorizationUrl(userId);

        await RespondAsync(
            InteractionCallback.Message(
                $"Click here to connect your Spotify account: {authUrl}\n\n" + $"_This connection is for **{Context.Guild.Name}** only._"
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
            await BlockUsageOutsideServer();
            return;
        }

        var connection = await dbContext.UserConnections.FirstOrDefaultAsync(u =>
            u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId
        );
        if (connection is null)
        {
            await RespondAsync(InteractionCallback.Message("❌ You don't have a connected Spotify account in this server."));
            return;
        }

        dbContext.UserConnections.Remove(connection);
        await dbContext.SaveChangesAsync();
        await RespondAsync(InteractionCallback.Message("✅ Your Spotify account has been disconnected from this server."));
    }

    [SlashCommand("stats", "View your Spotify listening stats")]
    public async Task StatsAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue)
        {
            await BlockUsageOutsideServer();
            return;
        }

        var connection = await dbContext
            .UserConnections.Include(u => u.SpotifyUser)
            .FirstOrDefaultAsync(u => u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId);
        if (connection is null || connection.SpotifyUser is null)
        {
            await RespondAsync(InteractionCallback.Message("❌ You haven't connected your Spotify yet! Use `/connect` first."));
            return;
        }

        await RespondAsync(InteractionCallback.DeferredMessage());

        try
        {
            // Refresh tokens if needed before making API calls
            await spotifyService.RefreshTokenIfNeeded(connection);

            var stats = await statsService.GenerateUserStats(guildId.Value, userId);
            await Context.Interaction.SendFollowupMessageAsync(stats);
        }
        catch (Exception ex)
        {
            logger.LogError($"Something went wrong when fetching stats: {ex.Message}", ex);
            await Context.Interaction.SendFollowupMessageAsync($"❌ Something went wrong when fetching your stats. Try again?");
        }
    }

    private async Task BlockUsageOutsideServer()
    {
        await RespondAsync(InteractionCallback.Message("❌ This command can only be used in a server!"));
    }
}
