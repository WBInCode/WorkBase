using MediatR;

namespace WorkBase.Shared.Domain;

/// <summary>
/// Marker interface for domain events dispatched via MediatR notifications.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base record for domain events with auto-set timestamp.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
