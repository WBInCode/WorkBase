using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Events;

public sealed record EmployeeCreatedEvent(Guid EmployeeId, Guid TenantId) : DomainEvent;

public sealed record EmployeeDeactivatedEvent(Guid EmployeeId, Guid TenantId) : DomainEvent;

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
