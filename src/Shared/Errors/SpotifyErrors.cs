using Listenfy.Shared.Results;

namespace Listenfy.Shared.Errors;

public static partial class Errors
{
    public class Spotify
    {
        public static Error AuthTimedOut => Error.Failure("Spotify.AuthTimedOut", "Invalid or expired OAuth state. Please run `/connect` again.");
        public static Error TokenExchangeFailed =>
            Error.Failure("Spotify.TokenExchangeFailed", "We weren't able to connect your account. Please run `/connect` again.");

        public static Error ProfileFetchFailed =>
            Error.Failure("Spotify.ProfileFetchFailed", "We weren't able to fetch your Spotify profile. Please try again.");

        public static Error AuthDenied =>
            Error.Failure("Spotify.AuthDenied", "You denied access to your Spotify account. Please run /connect again when you're ready.");
    }
}
