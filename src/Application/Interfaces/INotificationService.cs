namespace Listenfy.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Sends a direct message to a Discord user.
    /// </summary>
    /// <param name="userId">Discord user ID</param>
    /// <param name="message">Message content to send</param>
    /// <returns>True if message was sent successfully, false otherwise</returns>
    Task<bool> SendDirectMessageAsync(ulong userId, string message);

    /// <summary>
    /// Notifies a user that their Spotify connection was successful.
    /// </summary>
    /// <param name="userId">Discord user ID</param>
    /// <param name="guildName">Name of the guild where connection was made</param>
    Task NotifyConnectionSuccessAsync(ulong userId, string guildName);

    /// <summary>
    /// Notifies a user that their Spotify refresh token has expired and they need to reconnect.
    /// </summary>
    /// <param name="userId">Discord user ID</param>
    /// <param name="guildName">Name of the guild where connection expired</param>
    Task NotifyRefreshTokenExpiredAsync(ulong userId, string guildName);
}
