using FluentValidation;
using MediatR;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (errors.Count > 0)
            return CreateValidationResult(errors);

        return await next();
    }

    private static TResponse CreateValidationResult(IReadOnlyList<ValidationError> errors)
    {
        // Result (non-generic)
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)Result.ValidationFailure(errors);

        // Result<T> — find the T and call Result.ValidationFailure<T>()
        var resultType = typeof(TResponse).GetGenericArguments()[0];
        var method = typeof(Result)
            .GetMethod(nameof(Result.ValidationFailure), 1, [typeof(IReadOnlyList<ValidationError>)])!
            .MakeGenericMethod(resultType);

        return (TResponse)method.Invoke(null, [errors])!;
    }
}
