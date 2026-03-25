namespace WorkBase.Shared.Domain;

public abstract class AuditableEntity<TId> : Entity<TId>
    where TId : notnull
{
    public DateTime CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTime? ModifiedAt { get; protected set; }
    public string? ModifiedBy { get; protected set; }

    protected AuditableEntity() { }

    protected AuditableEntity(TId id) : base(id) { }

    public void SetCreated(DateTime timestamp, string? userId = null)
    {
        CreatedAt = timestamp;
        CreatedBy = userId;
    }

    public void SetModified(DateTime timestamp, string? userId = null)
    {
        ModifiedAt = timestamp;
        ModifiedBy = userId;
    }
}
