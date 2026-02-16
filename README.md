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

```bash
cp .env.example .env
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

### Database Setup

For local development, you can use Docker to run a PostgreSQL instance:

```bash
docker run --name listenfy-postgres \
  -e POSTGRES_PASSWORD=mysecretpassword \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_DB=ListenfyDb \
  -p 5430:5432 \
  -d postgres
```

This creates a PostgreSQL container with settings matching the `.env.example` file.

### Building

```bash
dotnet build
```

### Running

Ensure your `.env` file is configured and the database is running, then:

```bash
dotnet run
```

The bot will connect to Discord and be ready to use. Add the bot to your server using the OAuth2 URL with appropriate permissions:

- Send Messages
- Send Messages In Threads
- Embed Links
- Use Slash Commands
- Read Message History

### Code Formatting

This project uses [CSharpier](https://csharpier.com/) for code formatting. To check formatting:

```bash
dotnet tool restore
dotnet csharpier --check .
```

To format code:

```bash
dotnet csharpier .
```

## Contributing

Contributions are welcome! Whether it's bug fixes, new features, or documentation improvements, feel free to:

- Open pull requests for new features or improvements
- Report issues on GitHub
- Suggest enhancements

### Guidelines

- **Pull Requests**: PRs are squash merged, so ensure the PR title clearly describes the changes. The description should provide detailed context about what's being added or modified.
- **Commits**: Each commit within a PR should have an informative title. Please squash trivial commits before submitting.
- **Code Style**: Run `dotnet csharpier .` before committing to ensure code is properly formatted. The CI will check this automatically.
