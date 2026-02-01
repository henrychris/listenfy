using Listenfy.Application.Interfaces;

namespace Listenfy.Infrastructure.Services;

public class StatsService : IStatsService
{
    public Task<string> GenerateUserStats(ulong guildId, ulong userId)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateWeeklySummary(ulong guildId)
    {
        throw new NotImplementedException();
    }
}
