using Listenfy.Application.Settings;
using Listenfy.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Listenfy.Infrastructure.Configuration;

internal static class DatabaseConfiguration
{
    public static void SetupDatabase<T>(this IServiceCollection services)
        where T : DbContext
    {
        var logger = services.BuildServiceProvider().GetRequiredService<ILogger<T>>();

        var dbSettings = services.BuildServiceProvider().GetService<IOptionsSnapshot<DatabaseSettings>>()?.Value;
        var connectionString = Utilities.BuildConnectionString(dbSettings!);

        services.AddDbContext<T>(options => options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
        logger.LogInformation("Database registered.");
    }

    public static async Task ApplyMigrationsAsync<T>(this IHost host, string environment)
        where T : DbContext
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<T>>();

        if (environment != Environments.Production)
        {
            logger.LogInformation("Applying migrations...");
            var context = scope.ServiceProvider.GetRequiredService<T>();
            await context.Database.MigrateAsync();
            logger.LogInformation("Complete!");
        }
    }
}
