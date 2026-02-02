using System.Globalization;
using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Listenfy.Application.Jobs;

public class AggregateWeeklyStatsJob(ApplicationDbContext dbContext, TimeProvider timeProvider, ILogger<AggregateWeeklyStatsJob> logger)
{
    private const int TOP_ITEMS_TO_SHOW = 5;

    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting AggregateWeeklyStatsJob");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (weekIdentifier, weekStart, weekEnd) = CalculateCompletedWeek(now);

        logger.LogInformation(
            "Computing stats for week {WeekIdentifier} ({WeekStart:yyyy-MM-dd} to {WeekEnd:yyyy-MM-dd})",
            weekIdentifier,
            weekStart,
            weekEnd
        );

        // todo: process in batch - eventually. heh.
        var spotifyUsers = await dbContext.SpotifyUsers.ToListAsync();
        logger.LogInformation("Found {Count} Spotify users to process", spotifyUsers.Count);

        var processedCount = 0;
        var skippedCount = 0;

        foreach (var user in spotifyUsers)
        {
            try
            {
                var wasProcessed = await ProcessUserWeeklyStats(user, weekIdentifier, weekStart, weekEnd);
                if (wasProcessed)
                {
                    processedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing weekly stats for SpotifyUser {SpotifyUserId}", user.SpotifyUserId);
            }
        }

        logger.LogInformation("Completed AggregateWeeklyStatsJob. Processed: {Processed}, Skipped: {Skipped}", processedCount, skippedCount);
    }

    private async Task<bool> ProcessUserWeeklyStats(SpotifyUser user, string weekIdentifier, DateTime weekStart, DateTime weekEnd)
    {
        // Check if stats already exist for this week
        var existingStats = await dbContext.WeeklyStats.FirstOrDefaultAsync(ws => ws.SpotifyUserId == user.Id && ws.WeekIdentifier == weekIdentifier);
        if (existingStats is not null)
        {
            logger.LogInformation(
                "Stats already exist for user {SpotifyUserId}, week {WeekIdentifier}. Skipping.",
                user.SpotifyUserId,
                weekIdentifier
            );
            return false;
        }

        // Get listening history for the week
        var listeningHistory = await dbContext
            .ListeningHistories.Where(lh => lh.SpotifyUserId == user.Id && lh.PlayedAt >= weekStart && lh.PlayedAt <= weekEnd)
            .ToListAsync();
        if (listeningHistory.Count == 0)
        {
            logger.LogInformation(
                "No listening history for user {SpotifyUserId} in week {WeekIdentifier}. Skipping.",
                user.SpotifyUserId,
                weekIdentifier
            );
            return false;
        }

        // Compute top tracks
        var topTracks = listeningHistory
            .GroupBy(lh => new
            {
                lh.TrackId,
                lh.TrackName,
                lh.ArtistName,
            })
            .Select(g => new TopTrack
            {
                Name = g.Key.TrackName,
                Artist = g.Key.ArtistName,
                PlayCount = g.Count(),
            })
            .OrderByDescending(t => t.PlayCount)
            .Take(TOP_ITEMS_TO_SHOW)
            .ToList();

        // Compute top artists
        var topArtists = listeningHistory
            .SelectMany(lh => lh.ArtistName.Split(", ").Select(artist => new { Artist = artist.Trim(), Duration = lh.DurationMs }))
            .GroupBy(x => x.Artist)
            .Select(g => new TopArtist { Name = g.Key, PlayCount = g.Count() })
            .OrderByDescending(a => a.PlayCount)
            .Take(TOP_ITEMS_TO_SHOW)
            .ToList();

        // Compute aggregated stats
        var totalMinutes = listeningHistory.Sum(lh => lh.DurationMs) / 60000;
        var totalTracks = listeningHistory.Count;
        var uniqueTracks = listeningHistory.Select(lh => lh.TrackId).Distinct().Count();

        var weeklyStats = new WeeklyStat
        {
            SpotifyUserId = user.Id,
            WeekIdentifier = weekIdentifier,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            TopTracks = topTracks,
            TopArtists = topArtists,
            TotalMinutesListened = totalMinutes,
            TotalTracksPlayed = totalTracks,
            UniqueTracksPlayed = uniqueTracks,
            ComputedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        dbContext.WeeklyStats.Add(weeklyStats);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Created weekly stats for user {SpotifyUserId}, week {WeekIdentifier}. Tracks: {TotalTracks}, Minutes: {TotalMinutes}",
            user.SpotifyUserId,
            weekIdentifier,
            totalTracks,
            totalMinutes
        );
        return true;
    }

    /// <summary>
    /// Calculates the completed week (Monday to Sunday) based on ISO 8601.
    /// When run on Sunday, returns the week that just ended (previous Monday through Saturday night).
    /// </summary>
    private static (string WeekIdentifier, DateTime WeekStart, DateTime WeekEnd) CalculateCompletedWeek(DateTime now)
    {
        // Calculate the most recent Monday (start of the completed week)
        var daysSinceMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        if (daysSinceMonday == 0 && now.TimeOfDay.TotalHours < 1)
        {
            // If it's early Sunday morning (before 1am), go back to previous week
            daysSinceMonday = 7;
        }

        var weekStart = now.Date.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

        // ISO 8601 week number
        var calendar = CultureInfo.InvariantCulture.Calendar;
        var weekNumber = calendar.GetWeekOfYear(weekStart, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var year = weekStart.Year;

        // Handle edge case where week 1 might be in previous year
        if (weekNumber >= 52 && weekStart.Month == 1)
        {
            year--;
        }

        var weekIdentifier = $"{year}-W{weekNumber:D2}";

        return (weekIdentifier, weekStart, weekEnd);
    }
}
