namespace WorkBase.Modules.TimeTracking.Application.Dtos;

public sealed record TimeAnomalyDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    string Type,
    string Status,
    string? Description,
    string? Details,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime CreatedAt);
