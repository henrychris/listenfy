using Listenfy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Listenfy.Application.Events;

public class MessageCreateHandler(GatewayClient gatewayClient, IServiceProvider serviceProvider, ILogger<MessageCreateHandler> logger)
    : IGuildCreateGatewayHandler
{
    public async ValueTask HandleAsync(GuildCreateEventArgs args)
    {
        var guild = args.Guild;
        if (guild is null)
        {
            logger.LogError("Received GuildCreate event with null guild");
            return;
        }

        logger.LogInformation("Bot added to guild. Context: {@Context}", new { GuildName = guild.Name, DiscordGuildId = guild.Id });

        // Check if welcome message has already been sent
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var guildSettings = await dbContext.GuildSettings.FirstOrDefaultAsync(x => x.DiscordGuildId == guild.Id);
        if (guildSettings?.HasSentWelcomeMessage == true)
        {
            logger.LogInformation("Welcome message already sent for guild. Context: {@Context}", new { DiscordGuildId = guild.Id });
            return;
        }

        try
        {
            // Try to send message to system channel first
            var channelId = guild.SystemChannelId;

            // If no system channel, try to find first text channel we can send to
            if (!channelId.HasValue)
            {
                logger.LogInformation("No system channel found for guild. Context: {@Context}", new { DiscordGuildId = guild.Id });
                var channels = await guild.GetChannelsAsync();
                var firstTextChannel = channels.OfType<TextGuildChannel>().FirstOrDefault();
                if (firstTextChannel is null)
                {
                    logger.LogError(
                        "No suitable channel found to send welcome message in guild. Context: {@Context}",
                        new { DiscordGuildId = guild.Id }
                    );
                    return;
                }

                channelId = firstTextChannel.Id;
            }

            logger.LogInformation(
                "Sending welcome message to channel. Context: {@Context}",
                new { ChannelId = channelId.Value, DiscordGuildId = guild.Id }
            );

            var channel = await gatewayClient.Rest.GetChannelAsync(channelId.Value);
            if (channel is TextChannel textChannel)
            {
                var embeds = new[]
                {
                    new EmbedProperties
                    {
                        Title = "🎵 Listenfy Help",
                        Description = "Here's what you can do with Listenfy!",
                        Color = new Color(30, 215, 96), // Spotify Green
                        Fields =
                        [
                            new EmbedFieldProperties
                            {
                                Name = "🚀 First Time Setup",
                                Value =
                                    "1. Use `/connect` to link your Spotify account\n2. A server admin should use `/setchannel` to choose where weekly stats are posted\n3. Come back anytime to check `/personalstats` or `/serverstats`",
                            },
                            new EmbedFieldProperties
                            {
                                Name = "🎧 Personal Commands",
                                Value =
                                    "`/connect` - Link your Spotify account to this server\n`/disconnect` - Unlink your Spotify account from this server\n`/personalstats` - View your weekly listening statistics",
                            },
                            new EmbedFieldProperties
                            {
                                Name = "🏆 Server Commands",
                                Value =
                                    "`/serverstats` - See the top listeners in this server\n`/setchannel` - (Admin) Set where weekly stats are posted\n`/getchannel` - (Admin) See the current stats channel\n`/clearchannel` - (Admin) Remove the stats channel",
                            },
                            new EmbedFieldProperties { Name = "❓ Other", Value = "`/ping` - Pong!\n`/pong` - Ping!" },
                        ],
                    },
                };

                await textChannel.SendMessageAsync(new MessageProperties { Embeds = embeds });
                logger.LogInformation("Welcome message sent successfully to guild. Context: {@Context}", new { DiscordGuildId = guild.Id });

                // Mark welcome message as sent
                if (guildSettings is not null)
                {
                    guildSettings.HasSentWelcomeMessage = true;
                    dbContext.GuildSettings.Update(guildSettings);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Marked welcome message as sent for guild. Context: {@Context}", new { DiscordGuildId = guild.Id });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome message to guild. Context: {@Context}", new { DiscordGuildId = guild.Id });
        }
    }
}
