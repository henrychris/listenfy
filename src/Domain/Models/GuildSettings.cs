namespace Listenfy.Domain.Models;

public class GuildSettings : BaseEntity
{
    public ulong DiscordGuildId { get; set; }
    public required string GuildName { get; set; } // Discord server name (may become outdated)
    public ulong? StatsChannelId { get; set; } // Where to post weekly stats
    public bool IsEnabled { get; set; } = true;
    public bool HasSentWelcomeMessage { get; set; } = false;
    public DayOfWeek WeeklySummaryDay { get; set; } = DayOfWeek.Sunday;
    public TimeSpan WeeklySummaryTime { get; set; } = new TimeSpan(18, 0, 0); // 6 PM

    // Navigation property
    public ICollection<UserConnection> UserConnections { get; set; } = [];
}
