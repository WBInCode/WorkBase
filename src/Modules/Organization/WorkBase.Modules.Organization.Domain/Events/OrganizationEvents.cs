using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Events;

public sealed record EmployeeCreatedEvent(Guid EmployeeId, Guid TenantId) : DomainEvent;

public sealed record EmployeeDeactivatedEvent(Guid EmployeeId, Guid TenantId) : DomainEvent;

/// <summary>
/// Ponowne zatrudnienie albo cofnięcie omyłkowego zwolnienia.
/// </summary>
/// <remarks>
/// Zdarzenie dodane razem z odbieraniem dostępu przy zwolnieniu. Bez niego przywrócony pracownik
/// zostawałby z wyłączonym kontem i jedyną drogą powrotu przez konsolę Keycloaka — a nikt nie
/// skojarzyłby, że „przywróciłem go w kadrach" i „nie może się zalogować" to ta sama sprawa.
/// </remarks>
public sealed record EmployeeActivatedEvent(Guid EmployeeId, Guid TenantId) : DomainEvent;

public sealed record EmployeeAssignmentChangedEvent(
    Guid EmployeeId,
    Guid OrganizationUnitId,
    Guid PositionId,
    Guid TenantId) : DomainEvent;

public sealed record SupervisorChangedEvent(
    Guid SubordinateEmployeeId,
    Guid? NewSupervisorEmployeeId,
    Guid TenantId) : DomainEvent;

public sealed record OrganizationUnitCreatedEvent(Guid UnitId, Guid TenantId) : DomainEvent;

public sealed record OrganizationUnitUpdatedEvent(Guid UnitId, Guid TenantId) : DomainEvent;
