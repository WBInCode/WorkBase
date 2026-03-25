using MediatR;
using WorkBase.Shared.Domain;

namespace WorkBase.Shared.Cqrs;

/// <summary>
/// Query returning Result&lt;TResult&gt; (read operations).
/// </summary>
public interface IQuery<TResult> : IRequest<Result<TResult>>;

/// <summary>
/// Handler for IQuery&lt;TResult&gt;.
/// </summary>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>>
    where TQuery : IQuery<TResult>;
