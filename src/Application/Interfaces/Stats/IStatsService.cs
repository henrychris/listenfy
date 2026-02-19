using Listenfy.Shared.Results;
using NetCord.Rest;

namespace Listenfy.Application.Interfaces.Stats;

public interface IStatsService
{
    /// <summary>
    /// Gets the weekly stats for a specific user in a guild for the most recent completed week.
    /// Returns null if no stats exist.
    /// </summary>
    Task<Result<UserWeeklyStatsDto>> GetUserWeeklyStats(ulong guildId, ulong discordUserId);

    /// <summary>
    /// Gets weekly stats for all users in a guild for the most recent completed week.
    /// </summary>
    Task<Result<GuildWeeklyStatsDto>> GetGuildWeeklyStats(ulong guildId);

    /// <summary>
    /// Gets rolling last 7 days stats for all users in a guild.
    /// </summary>
    Task<Result<GuildWeeklyStatsDto>> GetGuildLast7DaysStats(ulong guildId);

    /// <summary>
    /// Builds a formatted Discord embed for a user's personal weekly stats.
    /// </summary>
    EmbedProperties BuildUserStatsEmbed(UserWeeklyStatsDto stats);

    /// <summary>
    /// Builds a formatted Discord embed for guild-wide weekly stats (top 5 users).
    /// </summary>
    EmbedProperties BuildGuildStatsEmbed(GuildWeeklyStatsDto guildStats, bool isLast7Days);
}
