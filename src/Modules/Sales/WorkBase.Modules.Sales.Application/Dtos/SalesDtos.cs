using WorkBase.Modules.Sales.Domain.Entities;

namespace WorkBase.Modules.Sales.Application.Dtos;

public sealed record LeadDto(
    Guid Id, string CompanyName, string? ContactName, string? Email, string? Phone,
    LeadSource Source, LeadStatus Status, Guid? AssigneeId, decimal? EstimatedValue,
    string? Notes, Guid? ContactId, DateTime CreatedAt, DateTime? ModifiedAt);

public sealed record OpportunityDto(
    Guid Id, string Name, Guid? ContactId, Guid? LeadId, Guid? AssigneeId,
    DealStage Stage, decimal Value, string Currency, int Probability,
    DateTime? ExpectedCloseDate, DateTime? ClosedAt, string? LostReason,
    string? Notes, DateTime CreatedAt, DateTime? ModifiedAt);

public sealed record OfferDto(
    Guid Id, Guid OpportunityId, string Number, string? Title,
    decimal TotalNet, decimal TotalGross, string Currency, OfferStatus Status,
    DateTime? ValidUntil, DateTime? SentAt, string? Notes, string ItemsJson,
    DateTime CreatedAt, DateTime? ModifiedAt);

public sealed record PipelineStageDto(
    Guid Id, string Name, string Color, int SortOrder, int DefaultProbability,
    bool IsClosedWon, bool IsClosedLost);

public sealed record PipelineSummaryDto(string StageName, string Color, int Count, decimal TotalValue);
