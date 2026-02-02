using Hangfire;
using Hangfire.PostgreSql;
using Listenfy.Application.Jobs;
using Listenfy.Application.Settings;
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

        recurringJobManager.AddOrUpdate<FetchListeningDataJob>(
            "fetch-listening-data",
            job => job.ExecuteAsync(),
            "*/20 * * * *", // every 20 mins
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc }
        );
    }
}
