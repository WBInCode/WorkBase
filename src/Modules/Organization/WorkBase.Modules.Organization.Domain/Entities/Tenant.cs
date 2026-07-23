using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Domain.Entities;

public sealed class Tenant : AuditableEntity<Guid>, IAuditable
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public string? Settings { get; private set; }

    /// <summary>Stable organization identifier assigned by HUB.</summary>
    public string? HubOrganizationId { get; private set; }

    /// <summary>Stable WorkBase product-instance identifier assigned by HUB.</summary>
    public string? HubProductInstanceId { get; private set; }

    /// <summary>
    /// Name of the Keycloak realm dedicated to this tenant (e.g. "tenant-acme-corp").
    /// Null while the tenant still lives on the shared "workbase" realm.
    /// See docs/05-module-licensing-architecture.md.
    /// </summary>
    public string? KeycloakRealmName { get; private set; }

    /// <summary>
    /// Optional reference to the <c>LicensePlan</c> (bundle of modules, e.g. Bronze/Silver/Gold)
    /// last applied to this tenant. Null means fully custom / no plan applied yet — the real,
    /// authoritative state of which modules are enabled always lives in FeatureFlag rows, not here.
    /// </summary>
    public Guid? LicensePlanId { get; private set; }

    public TenantStatus Status { get; private set; } = TenantStatus.Active;

    public DateTime? TrialExpiresAt { get; private set; }

    private Tenant() { }

    public static Tenant Create(string name, string slug)
    {
        return new Tenant
        {
            Name = name,
            Slug = slug,
            IsActive = true,
            Status = TenantStatus.Active
        };
    }

    public void Update(string name)
    {
        Name = name;
    }

    public void Deactivate()
    {
        IsActive = false;
        Status = TenantStatus.Suspended;
    }

    public void Activate()
    {
        IsActive = true;
        Status = TenantStatus.Active;
    }

    public void UpdateSettings(string? settings)
    {
        Settings = settings;
    }

    public void LinkToHub(string organizationId, string productInstanceId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("HUB organization id is required.", nameof(organizationId));
        if (string.IsNullOrWhiteSpace(productInstanceId))
            throw new ArgumentException("HUB product instance id is required.", nameof(productInstanceId));
        if (HubOrganizationId is not null && HubOrganizationId != organizationId)
            throw new InvalidOperationException("Tenant is already linked to another HUB organization.");
        if (HubProductInstanceId is not null && HubProductInstanceId != productInstanceId)
            throw new InvalidOperationException("Tenant is already linked to another HUB product instance.");

        HubOrganizationId = organizationId;
        HubProductInstanceId = productInstanceId;
    }

    public void AssignKeycloakRealm(string realmName)
    {
        if (string.IsNullOrWhiteSpace(realmName))
            throw new ArgumentException("Realm name is required.", nameof(realmName));
        KeycloakRealmName = realmName;
    }

    public void AssignLicensePlan(Guid? licensePlanId)
    {
        LicensePlanId = licensePlanId;
    }

    public void StartTrial(DateTime expiresAt)
    {
        Status = TenantStatus.Trial;
        TrialExpiresAt = expiresAt;
    }

    public void ConvertTrialToActive()
    {
        Status = TenantStatus.Active;
        TrialExpiresAt = null;
    }

    public void Cancel()
    {
        Status = TenantStatus.Cancelled;
        IsActive = false;
    }
}

/// <summary>
/// Lifecycle status of a tenant, independent of <see cref="Tenant.IsActive"/> which historically
/// only toggled visibility. Status drives access decisions (e.g. Suspended/Cancelled tenants
/// should be denied login even if IsActive was left true by mistake).
/// </summary>
public enum TenantStatus
{
    Trial = 0,
    Active = 1,
    Suspended = 2,
    Cancelled = 3
}
