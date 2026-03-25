namespace WorkBase.Shared.Cqrs;

/// <summary>
/// Marker interface for requests that require tenant context.
/// TenantId will be resolved from the current user's JWT claims.
/// </summary>
public interface ITenantRequest
{
    Guid TenantId { get; set; }
}
