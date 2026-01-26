using Listenfy.Application.Settings;

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
}
