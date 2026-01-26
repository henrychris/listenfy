# Discord Spotify Stats Bot - Complete Development Specification

## Project Overview

Build a Discord bot using .NET that tracks Spotify listening statistics for server members and posts weekly summaries.

## Technology Stack

- **NetCord** - Discord bot library (https://netcord.dev/)
- **Refit** - HTTP requests made easy
- **PostgreSQL** - Database with Entity Framework Core
- **Microsoft.Extensions.Hosting** - DI container and background services
- **Deployment** - Railway (with native PostgreSQL support)

## Project Structure

```
SpotifyStatsBot/
├── Program.cs                          # Entry point, DI setup, Railway config
├── Services/
│   ├── ISpotifyService.cs             # Interface for Spotify integration
│   ├── SpotifyService.cs              # Mocked implementation
│   ├── StatsService.cs                # Generates formatted stats summaries
│   └── WeeklyStatsScheduler.cs        # Background service for weekly posts
├── Commands/
│   ├── ConnectCommand.cs              # /connect - Links Spotify account
│   ├── StatsCommand.cs                # /stats - Shows user stats
│   ├── DisconnectCommand.cs           # /disconnect - Unlinks account
│   └── SetChannelCommand.cs           # /setchannel - Admin sets stats channel
├── Models/
│   ├── UserConnection.cs              # User-Spotify link with tokens
│   ├── GuildSettings.cs               # Per-server configuration
│   ├── ListeningStats.cs              # Stats data structure
│   ├── TopTrack.cs                    # Track info
│   └── TopArtist.cs                   # Artist info
├── Data/
│   ├── ApplicationDbContext.cs        # EF Core context
│   └── Migrations/                    # Auto-generated
├── appsettings.json                   # Configuration
└── appsettings.Development.json       # Local dev config
```

## Configuration Files

### appsettings.json

```json
{
  "Spotify": {
    "ClientId": "",
    "ClientSecret": "",
    "RedirectUri": "http://localhost:5000/callback"
  },
  "Scheduler": {
    "WeeklySummaryDay": "Sunday",
    "WeeklySummaryTime": "18:00"
  }
}
```

## Core Implementation

### Program.cs

```csharp

// Discord Client
builder.Services.AddSingleton(services =>
{
    return new GatewayClient(Token.CreateBot(discordToken), new GatewayClientConfiguration
    {
        Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
    });
});

// Application Commands
builder.Services.AddApplicationCommands<SlashCommandInteraction, SlashCommandContext>();

// Services
builder.Services.AddSingleton<ISpotifyService, SpotifyService>();
builder.Services.AddScoped<StatsService>();

// Background Services
builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<WeeklyStatsScheduler>();

var host = builder.Build();
await host.RunAsync();
```

### Services/ISpotifyService.cs

```csharp
public interface ISpotifyService
{
    Task<string> GetAuthorizationUrl(ulong discordUserId);
    Task<bool> CompleteAuthorization(string code, ulong discordUserId, ulong guildId);
    Task<ListeningStats> GetUserStats(ulong discordUserId, TimeSpan period);
    Task RefreshTokenIfNeeded(UserConnection connection);
}
```

### Services/SpotifyService.cs (MOCKED)

```csharp
public class SpotifyService : ISpotifyService
{
    private readonly Random _random = new();

    public Task<string> GetAuthorizationUrl(ulong discordUserId)
    {
        // Return a mocked URL - in reality this would be Spotify OAuth URL
        return Task.FromResult($"https://accounts.spotify.com/authorize?user={discordUserId} [MOCKED - Spotify API currently disabled]");
    }

    public Task<bool> CompleteAuthorization(string code, ulong discordUserId, ulong guildId)
    {
        // Mock successful authorization
        return Task.FromResult(true);
    }

    public Task<ListeningStats> GetUserStats(ulong discordUserId, TimeSpan period)
    {
        // Generate realistic mock data
        var mockArtists = new[] { "Taylor Swift", "The Weeknd", "Drake", "Bad Bunny", "Olivia Rodrigo", "Ed Sheeran" };
        var mockSongs = new[]
        {
            ("Anti-Hero", "Taylor Swift"),
            ("Blinding Lights", "The Weeknd"),
            ("One Dance", "Drake"),
            ("Tití Me Preguntó", "Bad Bunny"),
            ("vampire", "Olivia Rodrigo"),
            ("Shape of You", "Ed Sheeran")
        };

        var stats = new ListeningStats
        {
            DiscordUserId = discordUserId,
            TotalMinutesListened = _random.Next(300, 2000),
            TopTracks = mockSongs
                .OrderBy(_ => _random.Next())
                .Take(5)
                .Select(s => new TopTrack
                {
                    Name = s.Item1,
                    Artist = s.Item2,
                    PlayCount = _random.Next(10, 50)
                })
                .ToList(),
            TopArtists = mockArtists
                .OrderBy(_ => _random.Next())
                .Take(5)
                .Select(a => new TopArtist
                {
                    Name = a,
                    PlayCount = _random.Next(20, 100)
                })
                .ToList()
        };

        return Task.FromResult(stats);
    }

    public Task RefreshTokenIfNeeded(UserConnection connection)
    {
        // Mock - in reality this would refresh expired tokens
        return Task.CompletedTask;
    }
}
```

### Services/StatsService.cs

```csharp
using Microsoft.EntityFrameworkCore;
using System.Text;

public class StatsService
{
    private readonly ISpotifyService _spotifyService;
    private readonly ApplicationDbContext _dbContext;

    public StatsService(ISpotifyService spotifyService, ApplicationDbContext dbContext)
    {
        _spotifyService = spotifyService;
        _dbContext = dbContext;
    }

    public async Task<string> GenerateWeeklySummary(ulong guildId)
    {
        var connections = await _dbContext.UserConnections
            .Where(c => c.GuildId == guildId && c.SpotifyUserId is not null)
            .ToListAsync();

        if (!connections.Any())
        {
            return "No users have connected their Spotify accounts yet! Use `/connect` to get started.";
        }

        var summary = new StringBuilder();
        summary.AppendLine("📊 **Weekly Listening Stats**");
        summary.AppendLine($"*Stats for this server from {DateTime.UtcNow.AddDays(-7):MMM dd} - {DateTime.UtcNow:MMM dd}*\n");

        foreach (var connection in connections)
        {
            try
            {
                var stats = await _spotifyService.GetUserStats(
                    connection.DiscordUserId,
                    TimeSpan.FromDays(7)
                );

                summary.AppendLine($"**<@{connection.DiscordUserId}>**");

                if (stats.TopTracks.Any())
                {
                    var topTrack = stats.TopTracks.First();
                    summary.AppendLine($"🎵 Top Track: *{topTrack.Name}* by {topTrack.Artist} ({topTrack.PlayCount} plays)");
                }

                if (stats.TopArtists.Any())
                {
                    var topArtist = stats.TopArtists.First();
                    summary.AppendLine($"🎤 Top Artist: {topArtist.Name} ({topArtist.PlayCount} plays)");
                }

                summary.AppendLine($"⏱️ Total: {stats.TotalMinutesListened} minutes");
                summary.AppendLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching stats for user {connection.DiscordUserId}: {ex.Message}");
                summary.AppendLine($"**<@{connection.DiscordUserId}>** - _Could not fetch stats_\n");
            }
        }

        return summary.ToString();
    }

    public async Task<string> GenerateUserStats(ulong guildId, ulong userId)
    {
        var stats = await _spotifyService.GetUserStats(userId, TimeSpan.FromDays(7));

        var summary = new StringBuilder();
        summary.AppendLine("📊 **Your Listening Stats (Past 7 Days)**\n");

        summary.AppendLine("**Top Tracks:**");
        foreach (var track in stats.TopTracks.Take(5))
        {
            summary.AppendLine($"• *{track.Name}* by {track.Artist} - {track.PlayCount} plays");
        }

        summary.AppendLine("\n**Top Artists:**");
        foreach (var artist in stats.TopArtists.Take(5))
        {
            summary.AppendLine($"• {artist.Name} - {artist.PlayCount} plays");
        }

        summary.AppendLine($"\n⏱️ **Total listening time:** {stats.TotalMinutesListened} minutes");

        return summary.ToString();
    }
}
```

### Services/WeeklyStatsScheduler.cs

This will likely be replaced with Quartz or Hangfire, whichever allows the application to go to sleep while not in use.

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using NetCord.Gateway;
using NetCord.Rest;

public class WeeklyStatsScheduler : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
    private DateTime? _lastRunDate;

    public WeeklyStatsScheduler(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Weekly stats scheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndRunWeeklySummary();
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndRunWeeklySummary()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var guilds = await dbContext.GuildSettings
            .Where(g => g.IsEnabled && g.StatsChannelId is not null)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var guildSettings in guilds)
        {
            try
            {
                // Check if it's the right day/time for this guild
                if (now.DayOfWeek != guildSettings.WeeklySummaryDay)
                    continue;

                var todayAtTargetTime = now.Date + guildSettings.WeeklySummaryTime;
                var timeDifference = Math.Abs((now - todayAtTargetTime).TotalMinutes);

                // If within 30 minutes of target time and haven't run today
                if (timeDifference < 30 && !HasRunToday())
                {
                    await RunWeeklySummaryForGuild(guildSettings.GuildId, guildSettings.StatsChannelId.Value);
                    MarkAsRunToday();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing guild {guildSettings.GuildId}: {ex.Message}");
            }
        }
    }

    private async Task RunWeeklySummaryForGuild(ulong guildId, ulong channelId)
    {
        using var scope = _serviceProvider.CreateScope();
        var statsService = scope.ServiceProvider.GetRequiredService<StatsService>();
        var client = scope.ServiceProvider.GetRequiredService<GatewayClient>();

        var summary = await statsService.GenerateWeeklySummary(guildId);
        var channel = await client.Rest.GetTextChannelAsync(channelId);

        await channel.SendMessageAsync(new MessageProperties { Content = summary });

        Console.WriteLine($"Posted weekly stats for guild {guildId} in channel {channelId}");
    }

    private bool HasRunToday()
    {
        return _lastRunDate?.Date == DateTime.UtcNow.Date;
    }

    private void MarkAsRunToday()
    {
        _lastRunDate = DateTime.UtcNow;
    }
}
```

## Discord Commands

### Commands/ConnectCommand.cs

```csharp
using NetCord.Services.ApplicationCommands;
using Microsoft.EntityFrameworkCore;
using NetCord;

[SlashCommand("connect", "Connect your Spotify account to this server")]
public class ConnectCommand : ApplicationCommandModule<SlashCommandContext>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISpotifyService _spotifyService;

    public ConnectCommand(ApplicationDbContext dbContext, ISpotifyService spotifyService)
    {
        _dbContext = dbContext;
        _spotifyService = spotifyService;
    }

    public override async Task ExecuteAsync()
    {
        var guildId = Context.Interaction.GuildId;
        var userId = Context.Interaction.User.Id;

        if (!guildId.HasValue)
        {
            await RespondAsync(InteractionCallback.Message("❌ This command can only be used in a server!",
                MessageFlags.Ephemeral));
            return;
        }

        // Check if user already connected
        var existing = await _dbContext.UserConnections
            .FirstOrDefaultAsync(u => u.GuildId == guildId.Value && u.DiscordUserId == userId);

        if (existing is not null)
        {
            await RespondAsync(InteractionCallback.Message(
                "✅ You're already connected! Use `/disconnect` to unlink your account.",
                MessageFlags.Ephemeral));
            return;
        }

        // Ensure guild settings exist
        var guildSettings = await _dbContext.GuildSettings
            .FirstOrDefaultAsync(g => g.GuildId == guildId.Value);

        if (guildSettings is null)
        {
            guildSettings = new GuildSettings { GuildId = guildId.Value };
            _dbContext.GuildSettings.Add(guildSettings);
            await _dbContext.SaveChangesAsync();
        }

        // Create pending connection
        var connection = new UserConnection
        {
            GuildId = guildId.Value,
            DiscordUserId = userId,
            ConnectedAt = DateTime.UtcNow,
            SpotifyUserId = $"mock_spotify_{userId}" // Mock for now
        };

        _dbContext.UserConnections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var authUrl = await _spotifyService.GetAuthorizationUrl(userId);

        await RespondAsync(InteractionCallback.Message(
            $"🎵 **Spotify Connection**\n\n" +
            $"Your account has been connected (using mock data until Spotify API is available).\n\n" +
            $"When Spotify API is enabled, you'll click: {authUrl}\n\n" +
            $"_This connection is for **{Context.Guild.Name}** only._",
            MessageFlags.Ephemeral));
    }
}
```

## Database Migrations

After implementing the models, run:

```bash
dotnet ef migrations add InitialCreate
```

The migration will be applied automatically on startup via `Program.cs`.

## Railway Deployment

### Setup Steps

1. **Create Railway Project**
   - Sign up at railway.app
   - Create new project

2. **Add PostgreSQL Database**
   - Click "New" → "Database" → "PostgreSQL"
   - Railway auto-creates `DATABASE_URL` environment variable

3. **Deploy Bot**
   - Connect GitHub repository
   - Railway auto-detects .NET project
   - Add environment variables:
     - `DISCORD_TOKEN` - Your bot token from Discord Developer Portal
     - `ASPNETCORE_ENVIRONMENT` - Set to "Production"

4. **Discord Bot Setup**
   - Go to Discord Developer Portal (https://discord.com/developers/applications)
   - Create new application
   - Go to "Bot" section, create bot, copy token
   - Enable "Message Content Intent" if needed
   - Go to OAuth2 → URL Generator
   - Select scopes: `bot`, `applications.commands`
   - Select permissions: `Send Messages`, `Use Slash Commands`, `Read Message History`
   - Use generated URL to invite bot to your server

### Environment Variables on Railway

```
DATABASE_URL=(auto-generated by Railway)
DISCORD_TOKEN=your_discord_bot_token_here
ASPNETCORE_ENVIRONMENT=Production
```

## Testing Locally

1. **Setup PostgreSQL locally** (or use Docker):

```bash
docker run --name postgres-spotify -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres
```

2. **Update appsettings.Development.json**:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=spotifybot;Username=postgres;Password=postgres"
  },
  "Discord": {
    "Token": "your_test_bot_token"
  }
}
```

3. **Run migrations**:

```bash
dotnet ef database update
```

4. **Run the bot**:

```bash
dotnet run
```

## Bot Commands Summary

| Command    | Description                                | Permission |
| ---------- | ------------------------------------------ | ---------- |
| `/connect` | Connect your Spotify account to the server | Everyone   |
