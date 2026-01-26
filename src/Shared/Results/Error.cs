namespace Listenfy.Shared.Results;

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error Validation(string code, string description)
    {
        return new(code, description, ErrorType.Validation);
    }

    public static Error NotFound(string code, string description)
    {
        return new(code, description, ErrorType.NotFound);
    }

    public static Error Conflict(string code, string description)
    {
        return new(code, description, ErrorType.Conflict);
    }

    public static Error Unauthorized(string code, string description)
    {
        return new(code, description, ErrorType.Unauthorized);
    }

    public static Error Failure(string code, string description)
    {
        return new(code, description, ErrorType.Failure);
    }

    public static Error ServerError(string code, string description)
    {
        return new(code, description, ErrorType.ServerError);
    }

    public static Error Unexpected(string code, string description)
    {
        return new(code, description, ErrorType.Unexpected);
    }

    public static Error Forbidden(string code, string description)
    {
        return new(code, description, ErrorType.Forbidden);
    }
}

public enum ErrorType
{
    None,
    ServerError,
    Validation,
    NotFound,
    Conflict,
    Failure,
    Unauthorized,
    Unexpected,
    Forbidden,
}
