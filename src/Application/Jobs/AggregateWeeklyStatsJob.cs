using Listenfy.Domain.Models;
using Listenfy.Infrastructure.Persistence;
using Listenfy.Shared;
using Microsoft.EntityFrameworkCore;

namespace Listenfy.Application.Jobs;

public class AggregateWeeklyStatsJob(ApplicationDbContext dbContext, TimeProvider timeProvider, ILogger<AggregateWeeklyStatsJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting AggregateWeeklyStatsJob");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var (weekIdentifier, weekStart, weekEnd) = Utilities.CalculateCompletedWeek(now);

        logger.LogInformation(
            "Computing stats for week. Context: {@Context}",
            new
            {
                WeekIdentifier = weekIdentifier,
                WeekStart = $"{weekStart:yyyy-MM-dd}",
                WeekEnd = $"{weekEnd:yyyy-MM-dd}",
            }
        );

        // todo: process in batch - eventually. heh.
        var spotifyUsers = await dbContext.SpotifyUsers.ToListAsync();
        logger.LogInformation("Found Spotify users to process. Context: {@Context}", new { NumberOfUsers = spotifyUsers.Count });

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
                logger.LogError(ex, "Error processing weekly stats for SpotifyUser. Context: {@Context}", new { user.SpotifyUserId });
            }
        }

        logger.LogInformation("Completed AggregateWeeklyStatsJob. Context: {@Context}", new { Processed = processedCount, Skipped = skippedCount });
    }

    private async Task<bool> ProcessUserWeeklyStats(SpotifyUser user, string weekIdentifier, DateTime weekStart, DateTime weekEnd)
    {
        // Check if stats already exist for this week
        var existingStats = await dbContext.WeeklyStats.FirstOrDefaultAsync(ws => ws.SpotifyUserId == user.Id && ws.WeekIdentifier == weekIdentifier);
        if (existingStats is not null)
        {
            logger.LogInformation("Stats already exist for user. Context: {@Context}", new { user.SpotifyUserId, WeekIdentifier = weekIdentifier });
            return false;
        }

        // Get listening history for the week
        var listeningHistory = await dbContext
            .ListeningHistories.Where(lh => lh.SpotifyUserId == user.Id && lh.PlayedAt >= weekStart && lh.PlayedAt <= weekEnd)
            .ToListAsync();
        if (listeningHistory.Count == 0)
        {
            logger.LogInformation(
                "No listening history for user. Context: {@Context}. Skipping.",
                new { user.SpotifyUserId, WeekIdentifier = weekIdentifier }
            );
            return false;
        }

        var weeklyStat = Utilities.CalculateWeeklyStat(
            listeningHistory,
            user.Id,
            timeProvider.GetUtcNow().UtcDateTime,
            weekIdentifier,
            weekStart,
            weekEnd
        );
        dbContext.WeeklyStats.Add(weeklyStat);
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Created weekly stats for user. Context: {@Context}",
            new
            {
                user.SpotifyUserId,
                WeekIdentifier = weekIdentifier,
                TotalTracks = weeklyStat.TotalTracksPlayed,
                TotalMinutes = weeklyStat.TotalMinutesListened,
            }
        );
        return true;
    }
}
