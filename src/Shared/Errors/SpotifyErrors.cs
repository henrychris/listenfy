using Listenfy.Domain;
using Listenfy.Shared.Results;

namespace Listenfy.Shared.Errors;

public static partial class Errors
{
    public class Spotify
    {
        public static Error AuthTimedOut => Error.Failure("Spotify.AuthTimedOut", "Invalid or expired OAuth state. Please run `/connect` again.");

        public static Error TokenExchangeFailed =>
            Error.Failure("Spotify.TokenExchangeFailed", "We weren't able to connect your account. Please run `/connect` again.");

        public static Error TokenRefreshFailed =>
            Error.Failure(
                "Spotify.TokenRefreshFailed",
                "We weren't able to refresh your access token. Please run `disconnect`, then run `/connect` again."
            );

        public static Error ProfileFetchFailed =>
            Error.Failure("Spotify.ProfileFetchFailed", "We weren't able to fetch your Spotify profile. Please try again.");

        public static Error RecentlyPlayedTracksFetchFailed =>
            Error.Failure("Spotify.RecentlyPlayedTracksFetchFailed", "We weren't able to fetch your recently played tracks. Please try again.");

        public static Error AuthDenied =>
            Error.Failure("Spotify.AuthDenied", "You denied access to your Spotify account. Please run /connect again when you're ready.");
    }

    public class Stats
    {
        public static Error NotConnected =>
            Error.NotFound("Stats.NotConnected", "You haven't connected your Spotify account yet. Use `/connect` first.");

        public static Error NoStatsAvailable =>
            Error.NotFound(
                "Stats.NoStatsAvailable",
                $"No stats available yet. Try again in {StatMenuConstants.FETCH_DATA_JOB_INTERVAL_IN_MINUTES} minutes."
            );
    }
}
