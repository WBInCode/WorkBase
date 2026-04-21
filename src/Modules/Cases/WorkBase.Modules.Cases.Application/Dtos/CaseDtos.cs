using WorkBase.Modules.Cases.Domain.Entities;

namespace WorkBase.Modules.Cases.Application.Dtos;

public sealed record CaseItemDto(
    Guid Id, string Number, string Title, string? Description,
    Guid StatusId, string StatusName, string StatusColor,
    CasePriorityLevel Priority, Guid? AssigneeId, Guid? ContactId,
    Guid? CategoryId, string? CategoryName,
    DateTime? DueDate, DateTime? ResolvedAt, DateTime? ClosedAt,
    bool IsSlaBreached, DateTime CreatedAt, DateTime? ModifiedAt);

public sealed record CaseStatusDto(
    Guid Id, string Name, string Color, bool IsDefault, bool IsFinal, int SortOrder);

public sealed record CaseCategoryDto(
    Guid Id, string Name, string? Description, TimeSpan? DefaultSla);

public sealed record CaseCommentDto(
    Guid Id, Guid CaseId, Guid AuthorId, string Content, bool IsInternal, DateTime CreatedAt);
