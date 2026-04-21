using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Sales.Domain.Entities;

public enum LeadSource { Website, Referral, Campaign, ColdCall, Email, Social, Other }
public enum LeadStatus { New, Contacted, Qualified, Unqualified, Converted }
public enum DealStage { Qualification, Proposal, Negotiation, ClosedWon, ClosedLost }
public enum OfferStatus { Draft, Sent, Accepted, Rejected, Expired }

public sealed class Lead : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string CompanyName { get; private set; } = default!;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public LeadSource Source { get; private set; }
    public LeadStatus Status { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public decimal? EstimatedValue { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ContactId { get; private set; }

    private Lead() { }

    public static Lead Create(Guid tenantId, string companyName, LeadSource source, string? contactName = null,
        string? email = null, string? phone = null, Guid? assigneeId = null, decimal? estimatedValue = null)
    {
        var lead = new Lead
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyName = companyName,
            ContactName = contactName,
            Email = email,
            Phone = phone,
            Source = source,
            Status = LeadStatus.New,
            AssigneeId = assigneeId,
            EstimatedValue = estimatedValue
        };
        return lead;
    }

    public void Qualify() => Status = LeadStatus.Qualified;
    public void Disqualify() => Status = LeadStatus.Unqualified;
    public void MarkContacted() => Status = LeadStatus.Contacted;
    public void Convert(Guid contactId) { Status = LeadStatus.Converted; ContactId = contactId; }
    public void Assign(Guid assigneeId) => AssigneeId = assigneeId;
    public void Update(string companyName, string? contactName, string? email, string? phone, decimal? estimatedValue, string? notes)
    {
        CompanyName = companyName;
        ContactName = contactName;
        Email = email;
        Phone = phone;
        EstimatedValue = estimatedValue;
        Notes = notes;
    }
}

public sealed class Opportunity : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public Guid? ContactId { get; private set; }
    public Guid? LeadId { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DealStage Stage { get; private set; }
    public decimal Value { get; private set; }
    public string Currency { get; private set; } = "PLN";
    public int Probability { get; private set; }
    public DateTime? ExpectedCloseDate { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? LostReason { get; private set; }
    public string? Notes { get; private set; }

    private Opportunity() { }

    public static Opportunity Create(Guid tenantId, string name, decimal value, DealStage stage = DealStage.Qualification,
        Guid? contactId = null, Guid? leadId = null, Guid? assigneeId = null, int probability = 10,
        DateTime? expectedCloseDate = null, string currency = "PLN")
    {
        return new Opportunity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ContactId = contactId,
            LeadId = leadId,
            AssigneeId = assigneeId,
            Stage = stage,
            Value = value,
            Currency = currency,
            Probability = probability,
            ExpectedCloseDate = expectedCloseDate,
        };
    }

    public void AdvanceStage(DealStage stage, int probability)
    {
        Stage = stage;
        Probability = probability;
    }

    public void CloseWon() { Stage = DealStage.ClosedWon; Probability = 100; ClosedAt = DateTime.UtcNow; }
    public void CloseLost(string reason) { Stage = DealStage.ClosedLost; Probability = 0; ClosedAt = DateTime.UtcNow; LostReason = reason; }
    public void Update(string name, decimal value, string currency, DateTime? expectedCloseDate, string? notes)
    {
        Name = name; Value = value; Currency = currency; ExpectedCloseDate = expectedCloseDate; Notes = notes;
    }
}

public sealed class Offer : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public string Number { get; private set; } = default!;
    public string? Title { get; private set; }
    public decimal TotalNet { get; private set; }
    public decimal TotalGross { get; private set; }
    public string Currency { get; private set; } = "PLN";
    public OfferStatus Status { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? Notes { get; private set; }
    public string ItemsJson { get; private set; } = "[]";

    private Offer() { }

    public static Offer Create(Guid tenantId, Guid opportunityId, string number, string? title,
        decimal totalNet, decimal totalGross, string currency, DateTime? validUntil, string itemsJson)
    {
        return new Offer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OpportunityId = opportunityId,
            Number = number,
            Title = title,
            TotalNet = totalNet,
            TotalGross = totalGross,
            Currency = currency,
            Status = OfferStatus.Draft,
            ValidUntil = validUntil,
            ItemsJson = itemsJson
        };
    }

    public void Send() { Status = OfferStatus.Sent; SentAt = DateTime.UtcNow; }
    public void Accept() => Status = OfferStatus.Accepted;
    public void Reject() => Status = OfferStatus.Rejected;
    public void Expire() => Status = OfferStatus.Expired;
}

public sealed class PipelineStage : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Color { get; private set; } = "#3b82f6";
    public int SortOrder { get; private set; }
    public int DefaultProbability { get; private set; }
    public bool IsClosedWon { get; private set; }
    public bool IsClosedLost { get; private set; }

    private PipelineStage() { }

    public static PipelineStage Create(Guid tenantId, string name, string color, int sortOrder, int defaultProbability,
        bool isClosedWon = false, bool isClosedLost = false)
    {
        return new PipelineStage
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Name = name, Color = color,
            SortOrder = sortOrder, DefaultProbability = defaultProbability,
            IsClosedWon = isClosedWon, IsClosedLost = isClosedLost
        };
    }
}
