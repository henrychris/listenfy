using Hangfire;
using Hangfire.PostgreSql;
using Listenfy.Application.Jobs;
using Listenfy.Application.Settings;
using Listenfy.Domain;
using Listenfy.Shared;
using Microsoft.Extensions.Options;

namespace Listenfy.Infrastructure.Configuration;

internal static class HangfireConfiguration
{
    public static void SetupHangfire(this IServiceCollection services, string environment)
    {
        if (environment == "Test")
        {
            return;
        }

        var dbSettings = services.BuildServiceProvider().GetService<IOptionsSnapshot<DatabaseSettings>>()?.Value;
        if (dbSettings is null)
        {
            throw new InvalidOperationException("Database settings are not configured");
        }

        var connectionString = Utilities.BuildConnectionString(dbSettings);
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(post => post.UseNpgsqlConnection(connectionString));
        });
        services.AddHangfireServer();
    }

    public static void UseHangfireDashboard(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.UseHangfireDashboard("/hangfire");
    }

    public static void SetupRecurringJobs(this WebApplication app)
    {
        if (app.Environment.EnvironmentName == "Test")
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var spotifySettings = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<SpotifySettings>>()?.Value!;

        recurringJobManager.AddOrUpdate<FetchListeningDataJob>(
            "fetch-listening-data",
            job => job.ExecuteAsync(),
            $"*/{spotifySettings.FetchDataJobIntervalInMinutes} * * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );

        // Aggregate weekly stats every Sunday at 00:01 UTC
        recurringJobManager.AddOrUpdate<AggregateWeeklyStatsJob>(
            "aggregate-weekly-stats",
            job => job.ExecuteAsync(),
            "1 0 * * 0",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );

        // Send weekly stats every Sunday at 09:00 UTC
        recurringJobManager.AddOrUpdate<SendWeeklyStatsJob>(
            "send-weekly-stats",
            job => job.ExecuteAsync(),
            "0 9 * * 0",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );
    }
}
