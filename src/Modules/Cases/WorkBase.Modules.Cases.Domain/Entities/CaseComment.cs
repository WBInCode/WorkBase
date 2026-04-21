using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Cases.Domain.Entities;

public sealed class CaseComment : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid CaseId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = null!;
    public bool IsInternal { get; private set; }

    private CaseComment() { }

    public static CaseComment Create(Guid tenantId, Guid caseId, Guid authorId, string content, bool isInternal = false)
        => new() { TenantId = tenantId, CaseId = caseId, AuthorId = authorId, Content = content, IsInternal = isInternal };

    public void Update(string content) => Content = content;
}
