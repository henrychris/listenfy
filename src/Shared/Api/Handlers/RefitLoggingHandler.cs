namespace Listenfy.Shared.Api.Handlers;

public class RefitLoggingHandler(ILogger<RefitLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Log request
        var requestContent = string.Empty;
        if (request.Content != null)
        {
            requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        logger.LogInformation("Refit Request: {Method} {Uri} - Content: {Content}", request.Method, request.RequestUri, requestContent);

        var response = await base.SendAsync(request, cancellationToken);

        // Log response
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogInformation("Refit Response: {StatusCode} - Content: {Content}", response.StatusCode, responseContent);

        return response;
    }
}
