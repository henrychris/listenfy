# Listenfy - Discord Spotify Stats Bot

A Discord bot built with .NET that tracks Spotify listening statistics for server members and posts weekly summaries.

## Overview

Listenfy integrates Spotify with Discord to provide members with personalized listening statistics. Users can link their Spotify accounts, view their stats, and enjoy weekly summaries of their music listening habits. Server admins can configure where stats are posted.

## Technology Stack

- **Language**: C# (.NET 10.0)
- **Discord Integration**: [NetCord](https://netcord.dev/) - Modern Discord bot library
- **Database**: PostgreSQL with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

## Getting Started

### Prerequisites

- .NET 10.0 SDK
- PostgreSQL database
- Spotify API credentials (Client ID & Secret)
- Discord bot token

### Environment Variables

Copy `.env.example` to `.env` and update with your values:

```env
DiscordSettings__BotToken=your_discord_bot_token
ASPNETCORE_ENVIRONMENT=Development

DatabaseSettings__UserId=postgres
DatabaseSettings__Password=your_database_password
DatabaseSettings__Host=localhost
DatabaseSettings__Port=5432
DatabaseSettings__DatabaseName=ListenfyDb

SpotifySettings__ClientId=xxx
SpotifySettings__ClientSecret=xxx
SpotifySettings__ApiBaseUrl=xxx
SpotifySettings__AccountsBaseUrl=xxx
SpotifySettings__RedirectUrl=xxx

Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Override__Microsoft.AspNetCore.Mvc=Warning
Serilog__MinimumLevel__Override__Microsoft.AspNetCore.Routing=Warning
Serilog__MinimumLevel__Override__Microsoft.AspNetCore.Hosting=Warning
Serilog__MinimumLevel__Override__System.Net.Http.HttpClient.Refit=Warning
Serilog__MinimumLevel__Override__Microsoft.AspNetCore.Cors=Fatal
```

### Installation

1. Clone the repository:

```bash
git clone https://github.com/henrychris/listenfy.git
cd listenfy
```

2. Navigate to src directory:

```bash
cd src
```

3. Restore dependencies:

```bash
dotnet restore
```

4. Update database:

```bash
dotnet ef database update
```

5. Run the application:

```bash
dotnet run
```

The application will start at `http://localhost:5051`. The solitary callback endpoint is available at `http://localhost:5051/scalar/v1`.

## Development

### Building

```bash
dotnet build
```

## Contributing

Not accepting any at the moment. When I am, this section will be updated.
