using Listenfy.Application.Interfaces.Spotify;
using Listenfy.Application.Interfaces.Stats;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Microsoft.EntityFrameworkCore;
using NetCord;
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
            logger.LogError("Connect command invoked outside of a server. Context: {@Context}", new { DiscordUserId = userId, GuildId = guildId });
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Connect command started. Context: {@Context}", new { GuildId = guildId.Value, DiscordUserId = userId });

        // Check if user already has a COMPLETE connection
        var existingUserConnection = await dbContext
            .UserConnections.Include(u => u.SpotifyUser)
            .FirstOrDefaultAsync(u => u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId);
        if (existingUserConnection?.SpotifyUser is not null)
        {
            logger.LogInformation(
                "User already has a complete Spotify connection. Context: {@Context}",
                new { GuildId = guildId.Value, DiscordUserId = userId }
            );
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties
                    {
                        Content = "✅ You're already connected! Use `/disconnect` to unlink your account.",
                        Flags = MessageFlags.Ephemeral,
                    }
                )
            );
            return;
        }

        // If incomplete connection exists, delete it so we can start fresh
        if (existingUserConnection is not null)
        {
            logger.LogInformation(
                "Incomplete connection found, deleting. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = userId,
                    ExistingUserConnectionId = existingUserConnection.Id,
                }
            );
            dbContext.UserConnections.Remove(existingUserConnection);
            await dbContext.SaveChangesAsync();
        }

        // Ensure guild settings exist
        var guildSettings = await dbContext.GuildSettings.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId.Value);
        if (guildSettings is null)
        {
            logger.LogInformation(
                "Creating new GuildSettings during Connect. Context: {@Context}",
                new { GuildId = guildId.Value, GuildName = Context.Guild.Name }
            );
            guildSettings = new GuildSettings { DiscordGuildId = guildId.Value, GuildName = Context.Guild.Name };
            dbContext.GuildSettings.Add(guildSettings);
        }
        else if (guildSettings.GuildName != Context.Guild.Name)
        {
            // Update guild name if it has changed
            logger.LogInformation(
                "Updating guild name. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    OldGuildName = guildSettings.GuildName,
                    NewGuildName = Context.Guild.Name,
                }
            );
            guildSettings.GuildName = Context.Guild.Name;
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
            "Created pending UserConnection. Context: {@Context}",
            new
            {
                GuildId = guildId.Value,
                DiscordUserId = userId,
                ConnectionId = connection.Id,
                OAuthState = oAuthState,
            }
        );

        var authUrl = spotifyService.GetAuthorizationUrl(oAuthState);
        logger.LogInformation("Generated authorization URL for UserId. Context: {@Context}", new { DiscordUserId = userId, AuthUrl = authUrl });

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
                    Flags = MessageFlags.Ephemeral,
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
            logger.LogError("Disconnect command invoked outside of a server. Context: {@Context}", new { DiscordUserId = userId, GuildId = guildId });
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Disconnect command started. Context: {@Context}", new { GuildId = guildId.Value, DiscordUserId = userId });

        var connection = await dbContext.UserConnections.FirstOrDefaultAsync(u =>
            u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId
        );
        if (connection is null)
        {
            logger.LogError(
                "No Spotify connection found to disconnect. Context: {@Context}",
                new { GuildId = guildId.Value, DiscordUserId = userId }
            );
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties
                    {
                        Content = "❌ You don't have a connected Spotify account in this server.",
                        Flags = MessageFlags.Ephemeral,
                    }
                )
            );
            return;
        }

        logger.LogInformation(
            "Disconnecting Spotify account. Context: {@Context}",
            new
            {
                GuildId = guildId.Value,
                DiscordUserId = userId,
                ConnectionId = connection.Id,
            }
        );
        dbContext.UserConnections.Remove(connection);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Spotify account successfully disconnected. Context: {@Context}",
            new { GuildId = guildId.Value, DiscordUserId = userId }
        );
        await RespondAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties
                {
                    Content = "✅ Your Spotify account has been disconnected from this server.",
                    Flags = MessageFlags.Ephemeral,
                }
            )
        );
    }

    [SlashCommand("personalstats", "View your Spotify listening stats")]
    public async Task GetPersonalStatsAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue)
        {
            logger.LogError("Stats command invoked outside of a server. Context: {@Context}", new { DiscordUserId = userId, GuildId = guildId });
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Stats command started. Context: {@Context}", new { GuildId = guildId.Value, DiscordUserId = userId });

        var userConnection = await dbContext
            .UserConnections.Include(u => u.SpotifyUser)
            .FirstOrDefaultAsync(u => u.Guild.DiscordGuildId == guildId.Value && u.DiscordUserId == userId);
        if (userConnection is null || userConnection.SpotifyUser is null)
        {
            logger.LogError(
                "No Spotify connection found for stats. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = userId,
                    ConnectionExists = userConnection is not null,
                    SpotifyUserExists = userConnection?.SpotifyUser is not null,
                }
            );
            await RespondAsync(InteractionCallback.Message("❌ You haven't connected your Spotify yet! Use `/connect` first."));
            return;
        }

        logger.LogDebug(
            "Found Spotify connection. Context: {@Context}",
            new
            {
                GuildId = guildId.Value,
                DiscordUserId = userId,
                UserConnectionId = userConnection.Id,
            }
        );
        await RespondAsync(InteractionCallback.DeferredMessage());

        try
        {
            logger.LogInformation(
                "Refreshing tokens if needed. Context: {@Context}",
                new { DiscordUserId = userId, UserConnectionId = userConnection.Id }
            );
            // Refresh tokens if needed before making API calls
            await spotifyService.RefreshTokenIfNeeded(userConnection.SpotifyUser);

            logger.LogInformation(
                "Generating stats. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = userId,
                    UserConnectionId = userConnection.Id,
                }
            );
            var stats = await statsService.GetUserWeeklyStats(guildId.Value, userId);
            if (stats.IsFailure)
            {
                logger.LogError(
                    "No stats available for user. Context: {@Context}, Reason: {Reason}",
                    new
                    {
                        GuildId = guildId.Value,
                        DiscordUserId = userId,
                        UserConnectionId = userConnection.Id,
                    },
                    stats.Error.Description
                );
                await Context.Interaction.SendFollowupMessageAsync($"❌ {stats.Error.Description}");
                return;
            }

            var embed = statsService.BuildUserStatsEmbed(stats.Value);
            logger.LogInformation(
                "Stats generated successfully. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = userId,
                    UserConnectionId = userConnection.Id,
                }
            );
            await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties { Embeds = [embed] });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while fetching stats. Context: {@Context}",
                new
                {
                    GuildId = guildId.Value,
                    DiscordUserId = userId,
                    UserConnectionId = userConnection.Id,
                }
            );
            await Context.Interaction.SendFollowupMessageAsync($"❌ Something went wrong when fetching your stats. Try again?");
        }
    }

    [SlashCommand("serverstats", "View the top listeners within the server")]
    public async Task GetServerStatsAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue)
        {
            logger.LogError(
                "Server stats command invoked outside of a server. Context: {@Context}",
                new { DiscordUserId = userId, GuildId = guildId }
            );
            await InteractionGuards.BlockUsageOutsideServerAsync(Context);
            return;
        }

        logger.LogInformation("Server stats command started. Context: {@Context}", new { GuildId = guildId.Value, DiscordUserId = userId });
        await RespondAsync(InteractionCallback.DeferredMessage());

        try
        {
            var stats = await statsService.GetGuildLast7DaysStats(guildId.Value);
            if (stats.IsFailure)
            {
                logger.LogError(
                    "No server stats available. Context: {@Context}",
                    new
                    {
                        GuildId = guildId.Value,
                        DiscordUserId = userId,
                        Reason = stats.Error.Description,
                    }
                );
                await Context.Interaction.SendFollowupMessageAsync($"❌ {stats.Error.Description}");
                return;
            }

            var embed = statsService.BuildGuildStatsEmbed(stats.Value, true);
            logger.LogInformation(
                "Server stats generated successfully. Context: {@Context}",
                new { GuildId = guildId.Value, DiscordUserId = userId }
            );
            await Context.Interaction.SendFollowupMessageAsync(new InteractionMessageProperties { Embeds = [embed] });
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error occurred while fetching server stats. Context: {@Context}",
                new { GuildId = guildId.Value, DiscordUserId = userId }
            );
            await Context.Interaction.SendFollowupMessageAsync($"❌ Something went wrong when fetching server stats. Try again?");
        }
    }
}
