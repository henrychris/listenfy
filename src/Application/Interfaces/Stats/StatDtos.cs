namespace Listenfy.Application.Interfaces.Stats;

public class UserWeeklyStatsDto
{
    public required ulong DiscordUserId { get; set; }
    public required string WeekIdentifier { get; set; }
    public required DateTime WeekStartDate { get; set; }
    public required DateTime WeekEndDate { get; set; }
    public required List<TopTrackDto> TopTracks { get; set; }
    public required List<TopArtistDto> TopArtists { get; set; }
    public required int TotalMinutesListened { get; set; }
    public required int TotalTracksPlayed { get; set; }
    public required int UniqueTracksPlayed { get; set; }
    public required bool IncludesEarliestData { get; set; }
}

public class GuildWeeklyStatsDto
{
    public required string WeekIdentifier { get; set; }
    public required DateTime WeekStartDate { get; set; }
    public required DateTime WeekEndDate { get; set; }
    public required List<UserWeeklyStatsDto> UserStats { get; set; }
}

public class TopTrackDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ArtistDisplay { get; set; }
    public required int PlayCount { get; set; }
}

public class TopArtistDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int PlayCount { get; set; }
}
