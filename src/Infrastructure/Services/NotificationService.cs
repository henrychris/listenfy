using Listenfy.Application.Interfaces;
using NetCord.Gateway;
using NetCord.Rest;

namespace Listenfy.Infrastructure.Services;

public class NotificationService(GatewayClient discordClient, ILogger<NotificationService> logger) : INotificationService
{
    public async Task<bool> SendDirectMessageAsync(ulong userId, string message)
    {
        try
        {
            var user = await discordClient.Rest.GetUserAsync(userId);
            var dmChannel = await user.GetDMChannelAsync();
            await dmChannel.SendMessageAsync(new MessageProperties { Content = message });
            logger.LogInformation("Sent DM to user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send DM to user {UserId}", userId);
            return false;
        }
    }

    public async Task NotifyConnectionSuccessAsync(ulong userId, string guildName)
    {
        var message =
            $"✅ **Spotify Connected!**\n\n"
            + $"Your Spotify account has been successfully linked to **{guildName}**.\n\n"
            + $"We'll now track your listening history and you can view your stats anytime using `/personalstats`.\n\n"
            + $"_Note: It may take a few minutes before your first stats are available._";

        await SendDirectMessageAsync(userId, message);
    }

    public async Task NotifyRefreshTokenExpiredAsync(ulong userId, string guildName)
    {
        var message =
            $"⚠️ **Spotify Connection Expired**\n\n"
            + $"Your Spotify connection for **{guildName}** has expired and we can no longer track your listening history.\n\n"
            + $"To continue tracking your music, please reconnect:\n"
            + $"1. Go to **{guildName}**\n"
            + $"2. Run `/connect` to link your Spotify account again\n\n"
            + $"_Your previous stats are still saved!_";

        await SendDirectMessageAsync(userId, message);
    }
}
