namespace WorkBase.Modules.Organization.Application.Dtos;

/// <summary>Used by the platform-operator "companies" panel (docs/05-module-licensing-architecture.md step 5).</summary>
public sealed record TenantSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? KeycloakRealmName,
    Guid? LicensePlanId,
    string Status,
    bool IsActive,
    DateTime? TrialExpiresAt);

public sealed record OrganizationUnitDto(
    Guid Id,
    string Name,
    string? Code,
    Guid TypeId,
    string TypeName,
    Guid? ParentId,
    bool IsActive);

public sealed record OrganizationUnitTreeNodeDto(
    Guid Id,
    string Name,
    string? Code,
    Guid TypeId,
    string TypeName,
    bool IsActive,
    List<OrganizationUnitTreeNodeDto> Children);

public sealed record OrganizationUnitTypeDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);

public sealed record PositionDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);

public sealed record EmployeeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? EmployeeNumber,
    DateTime HireDate,
    DateTime? TerminationDate,
    string Status,
    Guid? UserId,
    decimal? HourlyRate,
    Guid? PrimaryOrganizationUnitId,
    string? PrimaryOrganizationUnitName);

public sealed record EmployeeDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? EmployeeNumber,
    DateTime HireDate,
    DateTime? TerminationDate,
    string Status,
    Guid? UserId,
    decimal? HourlyRate,
    List<EmployeeAssignmentDto> Assignments,
    SupervisorInfoDto? Supervisor);

public sealed record EmployeeAssignmentDto(
    Guid Id,
    Guid OrganizationUnitId,
    string OrganizationUnitName,
    Guid PositionId,
    string PositionName,
    bool IsPrimary,
    DateTime StartDate,
    DateTime? EndDate);

public sealed record SupervisorInfoDto(
    Guid EmployeeId,
    string FirstName,
    string LastName);

public sealed record PagedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
