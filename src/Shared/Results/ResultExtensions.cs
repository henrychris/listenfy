using FluentValidation.Results;
using Listenfy.Shared.Api.Responses;

namespace Listenfy.Shared.Results;

public static class ResultExtensions
{
    public static ApiResponse<T> ToSuccessfulApiResponse<T>(this Result<T> result)
    {
        return new ApiResponse<T>(data: result.Value, message: "Success", success: true);
    }

    public static List<Error> ToErrorList(this ValidationResult validationResult)
    {
        return [.. validationResult.Errors.Select(x => Error.Validation(x.ErrorCode, x.ErrorMessage))];
    }

    public static TResult Match<T, TResult>(this Result<T> result, Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
    }
}
