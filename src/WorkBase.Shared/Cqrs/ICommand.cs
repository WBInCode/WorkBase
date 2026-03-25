using MediatR;
using WorkBase.Shared.Domain;

namespace WorkBase.Shared.Cqrs;

/// <summary>
/// Marker for commands (write operations). Returns Result.
/// </summary>
public interface ICommand : IRequest<Result>;

/// <summary>
/// Command returning Result&lt;TResult&gt;.
/// </summary>
public interface ICommand<TResult> : IRequest<Result<TResult>>;

/// <summary>
/// Handler for ICommand (returns Result).
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Handler for ICommand&lt;TResult&gt; (returns Result&lt;TResult&gt;).
/// </summary>
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, Result<TResult>>
    where TCommand : ICommand<TResult>;
