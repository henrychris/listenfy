using System.Text.Json;

namespace Listenfy.Shared.Api.Responses;

public class ApiErrorResponse(List<ApiError> errors, string message)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public bool Success { get; init; }
    public string Message { get; init; } = message;
    public List<ApiError> Errors { get; init; } = errors;

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this, _jsonSerializerOptions);
    }
}
