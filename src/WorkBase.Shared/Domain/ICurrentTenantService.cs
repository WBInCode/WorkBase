namespace WorkBase.Shared.Domain;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
}
