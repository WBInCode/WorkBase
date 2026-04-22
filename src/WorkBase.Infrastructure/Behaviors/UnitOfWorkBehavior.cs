using MediatR;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Behaviors;

/// <summary>
/// Pipeline behavior that calls SaveChangesAsync after successful command execution.
/// Only runs for ICommand / ICommand&lt;T&gt; (write operations).
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    WorkBaseDbContext dbContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly bool IsCommand =
        typeof(TRequest).GetInterfaces().Any(i =>
            i == typeof(ICommand) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand)
            return await next();

        var response = await next();

        if (response.IsSuccess)
            await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }
}
