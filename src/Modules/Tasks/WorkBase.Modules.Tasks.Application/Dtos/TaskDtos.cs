namespace WorkBase.Modules.Tasks.Application.Dtos;

public sealed record TaskItemDto(
    Guid Id, string Title, string? Description,
    Guid StatusId, string StatusName, string? StatusColor,
    Guid PriorityId, string PriorityName, string? PriorityColor,
    Guid AssigneeId, Guid? ReporterId,
    DateTime? DueDate, DateTime? CompletedAt,
    DateTime CreatedAt,
    Guid? CoAssigneeId = null);

public sealed record TaskStatusDto(
    Guid Id, string Code, string Name, string? Color,
    bool IsFinal, bool IsDefault, int SortOrder);

public sealed record TaskPriorityDto(
    Guid Id, string Code, string Name, string? Color, int SortOrder);

public sealed record TaskCommentDto(
    Guid Id, Guid TaskId, Guid AuthorId,
    string Content, DateTime CreatedAt);

public sealed record TaskAttachmentDto(
    Guid Id, Guid TaskId, string FileName,
    string ContentType, long FileSizeBytes,
    Guid UploadedById, DateTime UploadedAt);

public sealed record TaskStatusTransitionDto(
    Guid Id, Guid FromStatusId, Guid ToStatusId);
