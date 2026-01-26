namespace Listenfy.Shared;

public static class DotEnv
{
    public static void Load(IServiceCollection services, string? environmentName = null)
    {
        using var scope = services.BuildServiceProvider().CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var root = Directory.GetCurrentDirectory();
        var parentDirectory = Directory.GetParent(root)?.FullName;

        // Special handling for test environment - navigate to the project root
        if (environmentName == "Test" && root.Contains("tests"))
        {
            // From tests/QBallTests/bin/Debug/net9.0, navigate to project root
            var current = root;
            while (current is not null && !File.Exists(Path.Combine(current, "ListenfyBot.sln")))
            {
                current = Directory.GetParent(current)?.FullName;
            }
            if (current is not null)
            {
                parentDirectory = current;
            }
        }

        if (string.IsNullOrEmpty(parentDirectory))
        {
            logger.LogError("Parent directory not found. Path: {Path}", root);
            throw new FileNotFoundException($"Parent directory not found. Path: {root}");
        }

        // Determine which .env file to load based on environment
        var envFileName = environmentName switch
        {
            "Test" => ".env.test",
            _ => ".env",
        };

        var filePath = Path.Combine(parentDirectory, envFileName);
        logger.LogInformation("Root directory: {root}", root);
        logger.LogInformation("Looking for {envFileName} file at: {filePath}", envFileName, filePath);

        if (!File.Exists(filePath))
        {
            logger.LogWarning("{envFileName} file not found at {filePath}", envFileName, filePath);
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                continue;
            }

            var equalIndex = line.IndexOf('=');
            if (equalIndex == -1)
            {
                continue;
            }

            var key = line.Substring(0, equalIndex);
            var value = line.Substring(equalIndex + 1);

            Environment.SetEnvironmentVariable(key, value);
        }

        logger.LogInformation("Environment variables loaded from {envFileName} file", envFileName);
    }
}
