namespace Listenfy.Application.Interfaces;

public interface IStatsService
{
    Task<string> GenerateWeeklySummary(ulong guildId);
    Task<string> GenerateUserStats(ulong guildId, ulong userId);
}
