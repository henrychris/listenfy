using System.Net;
using FluentValidation;
using Listenfy.Shared.Api.Responses;

namespace Listenfy.Application.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (ValidationException validationException)
        {
            await HandleValidationExceptions(httpContext, validationException);
            return;
        }
        catch (Exception exception)
        {
            LogException(httpContext, exception);
            await HandleExceptionAsync(httpContext);
        }
    }

    private async Task HandleValidationExceptions(HttpContext httpContext, ValidationException validationException)
    {
        logger.LogError("A validation exception occurred: {message}", validationException.Message);
        var apiErrors = validationException
            .Errors.Select(error => new ApiError { Code = error.ErrorCode, Description = error.ErrorMessage })
            .ToList();

        var response = new ApiErrorResponse(apiErrors, "One or more validation errors occurred");
        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        await httpContext.Response.WriteAsJsonAsync(response);
    }

    private void LogException(HttpContext httpContext, Exception ex)
    {
        var http = httpContext.GetEndpoint()?.DisplayName?.Split(" => ")[0] ?? httpContext.Request.Path.ToString();
        var httpMethod = httpContext.Request.Method;
        var type = ex.GetType().Name;
        var error = ex.Message;
        var inner =
            ex.InnerException != null
                ? $"""
                    -------------------------------
                    INNER EXCEPTION
                    {ex.InnerException}
                    """
                : string.Empty;

        var msg = $"""
            Something went wrong.
            =================================
            ENDPOINT: {http}
            METHOD: {httpMethod}
            TYPE: {type}
            REASON: {error}
            ---------------------------------
            STACK TRACE:
            {ex.StackTrace}
            {inner}
            """;

        logger.LogError("{@msg}", msg);
    }

    private static Task HandleExceptionAsync(HttpContext context)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var errors = new List<ApiError>
        {
            new() { Code = "System.InternalError", Description = "Something went wrong. Please reach out to an admin." },
        };

        var response = new ApiErrorResponse(errors, "Something went wrong. Please reach out to an admin.");
        return context.Response.WriteAsync(response.ToJsonString());
    }
}
