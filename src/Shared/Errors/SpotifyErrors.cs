using Listenfy.Shared.Results;

namespace Listenfy.Shared.Errors;

public static partial class Errors
{
    public class Spotify
    {
        public static Error AuthTimedOut => Error.Failure("Spotify.AuthTimedOut", "Invalid or expired OAuth state. Please run `/connect` again.");
    }
}
