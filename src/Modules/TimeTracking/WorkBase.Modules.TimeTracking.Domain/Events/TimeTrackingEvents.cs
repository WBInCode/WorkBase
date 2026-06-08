using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Domain.Events;

public sealed record ClockInRecordedEvent(
    Guid TimeEntryId,
    Guid TenantId,
    Guid EmployeeId,
    DateTime ClockInTime) : DomainEvent;

public sealed record ClockOutRecordedEvent(
    Guid TimeEntryId,
    Guid TenantId,
    Guid EmployeeId,
    DateTime ClockOutTime) : DomainEvent;

public sealed record BreakStartedEvent(
    Guid TimeEntryId,
    Guid TenantId,
    Guid EmployeeId,
    DateTime BreakStartTime) : DomainEvent;

public sealed record BreakEndedEvent(
    Guid TimeEntryId,
    Guid TenantId,
    Guid EmployeeId,
    DateTime BreakEndTime) : DomainEvent;

public sealed record AnomalyDetectedEvent(
    Guid AnomalyId,
    Guid TenantId,
    Guid EmployeeId,
    string AnomalyType,
    DateOnly Date) : DomainEvent;

public sealed record TimeCorrectedEvent(
    Guid CorrectionId,
    Guid TenantId,
    Guid EmployeeId,
    DateOnly Date,
    string CorrectedBy) : DomainEvent;

public sealed record OrgUnitScheduleChangedEvent(
    Guid OrgUnitScheduleId,
    Guid TenantId,
    Guid OrgUnitId) : DomainEvent;
