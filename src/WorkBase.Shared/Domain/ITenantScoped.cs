namespace WorkBase.Shared.Domain;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
