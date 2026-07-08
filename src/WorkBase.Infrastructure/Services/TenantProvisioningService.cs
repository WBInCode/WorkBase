using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Auth.MultiRealm;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Services;

public sealed class TenantProvisioningService(
    WorkBaseDbContext dbContext,
    IKeycloakAdminService keycloakAdmin,
    TenantIssuerCache issuerCache,
    IConfiguration configuration,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task<TenantProvisioningResult> CreateTenantAsync(
        string name,
        string slug,
        string adminEmail,
        string adminFirstName,
        string adminLastName,
        CancellationToken cancellationToken = default)
    {
        var tenant = Tenant.Create(name, slug);
        dbContext.Set<Tenant>().Add(tenant);

        // Tenant.Id is database/convention-generated (UUID v7) on insert — populated on the
        // tracked entity after SaveChangesAsync, so it's safe to read tenant.Id below.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Translated to a plain BCL exception here (Infrastructure layer, where EF Core
            // types belong) so callers in other layers (e.g. Organization.Api, which doesn't
            // reference EF Core) can catch it without a new package dependency.
            throw new InvalidOperationException($"Tenant with slug '{slug}' already exists.", ex);
        }

        logger.LogInformation("Created tenant {TenantId} ({Name}/{Slug})", tenant.Id, name, slug);

        // A brand-new tenant has no Roles/Permissions/DataScopes of its own yet — without
        // this, its first provisioned user would get zero working permissions (see
        // UserProvisioningService.GetDefaultRoleIdAsync).
        await IamSeeder.SeedTenantRbacAsync(dbContext, tenant.Id, logger);

        // Multi-realm mode (docs/05-module-licensing-architecture.md §5): every new company
        // gets its own, fully login-ready Keycloak realm. Realm-per-tenant means the issuer
        // itself identifies the company — tokens from this realm get their tenant_id claim
        // overridden from the issuer mapping (see AuthenticationExtensions), so nothing a
        // user or realm-level admin edits inside the realm can point at another company.
        var multiRealmEnabled = configuration.GetValue<bool>("Keycloak:MultiRealmEnabled");
        string? realmName = null;

        if (multiRealmEnabled)
        {
            realmName = $"tenant-{SanitizeRealmName(slug)}";
            var redirectUris = GetSpaRedirectUris();

            var realmCreated = await keycloakAdmin.CreateTenantRealmAsync(realmName, name, redirectUris, cancellationToken);
            if (realmCreated)
            {
                tenant.AssignKeycloakRealm(realmName);
                await dbContext.SaveChangesAsync(cancellationToken);

                // Accept logins from the new realm immediately — don't make the freshly
                // onboarded admin wait for the next background issuer-cache refresh.
                issuerCache.RegisterIssuer(realmName, tenant.Id);
            }
            else
            {
                logger.LogWarning(
                    "Tenant {TenantId}: dedicated realm {Realm} could not be created — falling back to the shared realm.",
                    tenant.Id, realmName);
                realmName = null;
            }
        }

        // Provision the company's first admin: Keycloak account (temporary password, forced
        // change on first login) + linked application user with the tenant's own Admin role.
        // In multi-realm mode the account lives in the tenant's DEDICATED realm and gets the
        // workbase-admin realm role (frontend nav/role checks); otherwise it goes to the
        // shared realm, where the tenant_id attribute (same mapper as employee provisioning)
        // scopes every request to the new company.
        var temporaryPassword = GenerateTemporaryPassword();
        var adminAttributes = new Dictionary<string, string> { ["tenant_id"] = tenant.Id.ToString() };

        var keycloakUserId = realmName is not null
            ? await keycloakAdmin.CreateUserInRealmAsync(
                realmName, adminEmail, adminFirstName, adminLastName, temporaryPassword,
                adminAttributes, realmRoles: ["workbase-admin"], cancellationToken)
            : await keycloakAdmin.CreateUserAsync(
                adminEmail, adminFirstName, adminLastName, temporaryPassword,
                adminAttributes, cancellationToken);

        if (keycloakUserId is null)
        {
            // Keycloak unreachable/unconfigured — the tenant itself is fine; the operator
            // must create the admin account manually. Never fail the whole onboarding here.
            logger.LogWarning(
                "Tenant {TenantId} created, but Keycloak admin account for {Email} could not be provisioned.",
                tenant.Id, adminEmail);
            return new TenantProvisioningResult(tenant.Id, adminEmail, null, realmName);
        }

        var adminUser = User.Create(keycloakUserId, adminEmail, adminFirstName, adminLastName, tenant.Id);
        dbContext.Set<User>().Add(adminUser);

        var adminRoleId = await dbContext.Set<Role>()
            .Where(r => r.TenantId == tenant.Id && r.Name == "Admin")
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId is not null)
        {
            dbContext.Set<UserRole>().Add(
                UserRole.Create(adminUser.Id, adminRoleId.Value, tenant.Id, "tenant-onboarding"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Provisioned admin {Email} (Keycloak: {KeycloakId}) for tenant {TenantId}",
            adminEmail, keycloakUserId, tenant.Id);

        return new TenantProvisioningResult(tenant.Id, adminEmail, temporaryPassword, realmName);
    }

    /// <summary>Realm names must be URL-safe — keep lowercase letters, digits and hyphens only.</summary>
    private static string SanitizeRealmName(string slug)
    {
        var sanitized = new string(slug.ToLowerInvariant()
            .Select(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-' ? c : '-')
            .ToArray()).Trim('-');
        return string.IsNullOrEmpty(sanitized) ? throw new InvalidOperationException($"Slug '{slug}' cannot be converted to a valid realm name.") : sanitized;
    }

    /// <summary>
    /// Redirect URIs for the tenant realm's SPA client — derived from the CORS allow-list
    /// (the same origins the SPA is actually served from) plus local dev hosts.
    /// </summary>
    private string[] GetSpaRedirectUris()
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        return origins
            .Concat(["http://localhost:5173", "http://localhost:5174", "http://localhost:5175"])
            .Distinct()
            .Select(o => $"{o.TrimEnd('/')}/*")
            .ToArray();
    }

    /// <summary>
    /// Cryptographically random, 16-char temporary password (Keycloak marks it temporary, so
    /// the admin must change it at first login). Alphabet avoids ambiguous characters and
    /// includes all four classes to satisfy typical password policies.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^*";
        const string all = upper + lower + digits + special;

        var chars = new char[16];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Shuffle so the guaranteed-class characters aren't always in the same positions.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
