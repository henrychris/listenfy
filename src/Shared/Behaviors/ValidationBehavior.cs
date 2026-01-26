using FluentValidation;
using MediatR;

namespace Listenfy.Shared.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogDebug("In validation pipeline.");
        if (!validators.Any())
        {
            logger.LogDebug("No validators registered");
            return await next();
        }

        logger.LogDebug("Found {count} validators: {validators}", validators.Count(), string.Join(", ", validators.Select(v => v.GetType().Name)));
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.Where(r => !r.IsValid).SelectMany(r => r.Errors).ToList();
        return failures.Count > 0 ? throw new ValidationException(failures) : await next();
    }
}
