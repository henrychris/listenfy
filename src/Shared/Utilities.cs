using System.Globalization;
using Listenfy.Application.Settings;
using Listenfy.Domain;
using Listenfy.Domain.Models;

namespace Listenfy.Shared;

public static class Utilities
{
    public static string BuildConnectionString(DatabaseSettings databaseSettings)
    {
        if (!string.IsNullOrEmpty(databaseSettings.ConnectionString))
        {
            return databaseSettings.ConnectionString;
        }

        return $"User ID={databaseSettings.UserId}; Password={databaseSettings.Password}; Host={databaseSettings.Host}; Port={databaseSettings.Port}; Database={databaseSettings.DatabaseName}; Pooling=true;";
    }

    /// <summary>
    /// Calculates the completed week (Monday to Sunday) based on ISO 8601.
    /// When run on Sunday, returns the week that just ended (previous Monday through Saturday night).
    /// </summary>
    public static (string weekIdentifier, DateTime weekStart, DateTime weekEnd) CalculateCompletedWeek(DateTime now)
    {
        // Calculate the most recent completed week (Monday to Sunday)
        var daysSinceMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (daysSinceMonday == 0)
        {
            // If it's Monday, use last week (completed on Sunday)
            daysSinceMonday = 7;
        }

        var weekStart = now.Date.AddDays(-daysSinceMonday); // Most recent Monday
        var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59); // Sunday 23:59:59

        var calendar = CultureInfo.InvariantCulture.Calendar;
        var weekNumber = calendar.GetWeekOfYear(weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var year = weekStart.Year;

        if (weekNumber >= 52 && weekStart.Month == 1)
        {
            year--;
        }

        var weekIdentifier = $"{year}-W{weekNumber:D2}";
        return (weekIdentifier, weekStart, weekEnd);
    }

    public static string CalculateWeekIdentifier(DateTime now)
    {
        var daysSinceMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (daysSinceMonday == 0 && now.TimeOfDay.TotalHours < 9)
        {
            daysSinceMonday = 7;
        }

        var weekStart = now.Date.AddDays(-daysSinceMonday);

        var calendar = CultureInfo.InvariantCulture.Calendar;
        var weekNumber = calendar.GetWeekOfYear(weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var year = weekStart.Year;

        if (weekNumber >= 52 && weekStart.Month == 1)
        {
            year--;
        }

        return $"{year}-W{weekNumber:D2}";
    }

    public static WeeklyStat CalculateWeeklyStat(
        List<ListeningHistory> listeningHistory,
        string spotifyUserId,
        DateTime nowUtc,
        string weekIdentifier,
        DateTime weekStart,
        DateTime weekEnd
    )
    {
        // Compute top tracks
        var topTracks = listeningHistory
            .GroupBy(lh => new
            {
                lh.TrackId,
                lh.TrackName,
                ArtistName = string.Join(", ", lh.ArtistNames),
            })
            .Select(g => new TopTrack
            {
                Name = g.Key.TrackName,
                Artist = g.Key.ArtistName,
                PlayCount = g.Count(),
            })
            .OrderByDescending(t => t.PlayCount)
            .Take(StatMenuConstants.TOP_ITEMS_TO_SHOW)
            .ToList();

        // Compute top artists - count each artist from the list
        // Each listening history entry counts as 1 play for each artist on that track
        var topArtists = listeningHistory
            .SelectMany(lh => lh.ArtistNames)
            .GroupBy(artist => artist)
            .Select(g => new TopArtist { Name = g.Key, PlayCount = g.Count() })
            .OrderByDescending(a => a.PlayCount)
            .Take(StatMenuConstants.TOP_ITEMS_TO_SHOW)
            .ToList();

        // Compute aggregated stats
        var totalMinutes = listeningHistory.Sum(lh => lh.DurationMs) / 60000;
        var totalTracks = listeningHistory.Count;
        var uniqueTracks = listeningHistory.Select(lh => lh.TrackId).Distinct().Count();

        return new WeeklyStat
        {
            SpotifyUserId = spotifyUserId,
            WeekIdentifier = weekIdentifier,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            TopTracks = topTracks,
            TopArtists = topArtists,
            TotalMinutesListened = totalMinutes,
            TotalTracksPlayed = totalTracks,
            UniqueTracksPlayed = uniqueTracks,
            ComputedAt = nowUtc,
        };
    }
}
