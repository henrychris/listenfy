using System.Net.Mime;
using Listenfy.Shared.Api.Responses;
using Listenfy.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace Listenfy.Shared.Api;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Returns an IActionResult object based on the list of errors passed as parameter.
    /// </summary>
    /// <param name="errors">List of errors to be handled.</param>
    /// <returns>An IActionResult object based on the type of errors.</returns>
    protected static IActionResult ReturnErrorResponse(List<Error> errors)
    {
        if (errors == null || errors.Count == 0)
        {
            throw new ArgumentException("Errors list cannot be null or empty", nameof(errors));
        }

        var errorMessage = DetermineErrorMessage(errors);
        var statusCode = DetermineStatusCode(errors[0].Type);
        var apiErrors = errors.Select(x => new ApiError { Code = x.Code, Description = x.Description }).ToList();
        var problemDetails = new ApiErrorResponse(apiErrors, errorMessage);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    protected static IActionResult ReturnErrorResponse(Error error)
    {
        var errorMessage = GetSingleErrorMessage(error.Type);
        var statusCode = DetermineStatusCode(error.Type);

        var apiError = new ApiError { Code = error.Code, Description = error.Description };

        var problemDetails = new ApiErrorResponse([apiError], errorMessage);
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Ok(result.Value) : ReturnErrorResponse(result.Error);
    }

    private static string DetermineErrorMessage(List<Error> errors)
    {
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            return "One or more validation errors occurred.";
        }

        if (errors.Any(e => e.Type == ErrorType.Unexpected))
        {
            return "An unexpected error occurred.";
        }

        if (errors.Count == 1)
        {
            return GetSingleErrorMessage(errors[0].Type);
        }

        return "Multiple errors occurred.";
    }

    private static string GetSingleErrorMessage(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.NotFound => "The requested resource was not found.",
            ErrorType.Validation => "A validation error occurred.",
            ErrorType.Conflict => "A conflict occurred.",
            ErrorType.Failure => "The request failed.",
            ErrorType.Unauthorized => "Unauthorized access.",
            ErrorType.ServerError => "An internal server error occurred.",
            ErrorType.Unexpected => "An unexpected error occurred.",
            ErrorType.Forbidden => "Access denied.",
            _ => "An error occurred.",
        };
    }

    private static int DetermineStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.ServerError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
