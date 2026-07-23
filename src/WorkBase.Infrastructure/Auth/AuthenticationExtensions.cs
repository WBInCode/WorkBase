using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WorkBase.Infrastructure.Auth.MultiRealm;
using WorkBase.Infrastructure.HubPlatform;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Auth;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddWorkBaseAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"]!;
        var audience = configuration["Keycloak:Audience"]!;
        var requireHttps = configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
        var metadataAddress = configuration["Keycloak:MetadataAddress"];

        // Multi-realm Keycloak (docs/05-module-licensing-architecture.md step 6). Disabled by
        // default (Keycloak:MultiRealmEnabled unset/false) — every existing deployment keeps
        // using the framework's built-in single-Authority validation below, byte-for-byte
        // unchanged. Only flip this on after a dedicated security review (see
        // DynamicIssuerValidation's doc comment for what needs re-checking first).
        var multiRealmEnabled = configuration.GetValue<bool>("Keycloak:MultiRealmEnabled");

        // The issuer cache is registered unconditionally: TenantProvisioningService injects it
        // to register freshly created realms immediately. It stays empty (and unused by token
        // validation) while multi-realm is disabled.
        services.AddSingleton<TenantIssuerCache>();

        if (multiRealmEnabled)
        {
            services.AddSingleton<DynamicIssuerValidation>();
            services.AddHostedService<TenantIssuerCacheRefreshService>();
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttps;
                options.MapInboundClaims = false;

                if (multiRealmEnabled)
                {
                    // Do NOT set options.Authority / MetadataAddress here: that would make the
                    // framework fetch a single realm's metadata and fight with our own dynamic
                    // per-issuer resolution below.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = "roles",
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var dynamicValidation = context.HttpContext.RequestServices.GetRequiredService<DynamicIssuerValidation>();
                            context.Options.TokenValidationParameters.IssuerValidator = dynamicValidation.ValidateIssuer;
                            context.Options.TokenValidationParameters.IssuerSigningKeyResolver = dynamicValidation.ResolveSigningKeys;
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async context =>
                        {
                            MapKeycloakClaims(context);
                            OverrideTenantClaimFromIssuer(context);

                            if (!await ValidateTenantAccessAsync(context))
                                return;

                            if (context.Principal is not null)
                            {
                                var provisioned = await UserProvisioningService.OnTokenValidatedAsync(
                                    context.HttpContext.RequestServices,
                                    context.Principal);
                                if (!provisioned && context.Principal.HasClaim(claim => claim.Type == "hub_role"))
                                    context.Fail("HUB role synchronization failed.");
                            }
                        }
                    };
                }
                else
                {
                    options.Authority = authority;

                    if (!string.IsNullOrEmpty(metadataAddress))
                    {
                        options.MetadataAddress = metadataAddress;
                    }

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = authority,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = "roles"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            MapKeycloakClaims(context);

                            if (!await ValidateTenantAccessAsync(context))
                                return;

                            if (context.Principal is not null)
                            {
                                var provisioned = await UserProvisioningService.OnTokenValidatedAsync(
                                    context.HttpContext.RequestServices,
                                    context.Principal);
                                if (!provisioned && context.Principal.HasClaim(claim => claim.Type == "hub_role"))
                                    context.Fail("HUB role synchronization failed.");
                            }
                        }
                    };
                }
            });

        services.AddAuthorization();

        return services;
    }

    private static void MapKeycloakClaims(TokenValidatedContext context)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity is null)
            return;

        // Ensure 'sub' is mapped to NameIdentifier for consistent access
        var sub = identity.FindFirst("sub")?.Value;
        if (sub is not null && !identity.HasClaim(ClaimTypes.NameIdentifier, sub))
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub));
        }
    }

    private static async Task<bool> ValidateTenantAccessAsync(TokenValidatedContext context)
    {
        var tenantClaim = context.Principal?.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantClaim, out var tenantId))
        {
            context.Fail("Tenant context is required.");
            return false;
        }

        await using var scope = context.HttpContext.RequestServices.CreateAsyncScope();
        var accessCache = scope.ServiceProvider.GetRequiredService<TenantAccessCache>();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkBaseDbContext>();
        TenantAccessState tenantAccess;
        if (accessCache.TryGet(tenantId, out var cachedState) && cachedState is not null)
        {
            tenantAccess = cachedState;
        }
        else
        {
            var tenant = await dbContext.Set<Tenant>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(item => item.Id == tenantId)
                .Select(item => new
                {
                    item.IsActive,
                    item.Status,
                    item.TrialExpiresAt,
                    item.HubProductInstanceId,
                })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            var trialActive = tenant?.Status == TenantStatus.Trial
                              && tenant.TrialExpiresAt is not null
                              && tenant.TrialExpiresAt > DateTime.UtcNow;
            var accessAllowed = tenant is not null
                                && tenant.IsActive
                                && (tenant.Status == TenantStatus.Active || trialActive);
            tenantAccess = new TenantAccessState(accessAllowed, tenant?.HubProductInstanceId);
            accessCache.Set(tenantId, tenantAccess);
        }

        if (!tenantAccess.AccessAllowed)
        {
            context.Fail("Tenant access is inactive.");
            return false;
        }

        var isKiosk = context.Principal?.IsInRole("workbase-kiosk") == true;
        var hubOptions = scope.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection(HubOptions.SectionName)
            .Get<HubOptions>() ?? new HubOptions();
        if (tenantAccess.HubProductInstanceId is not null
            && hubOptions.UserAccessCheckEnabled
            && !isKiosk)
        {
            var email = context.Principal?.FindFirstValue("email");
            var hubUserId = context.Principal?.FindFirstValue("hub_user_id");
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(hubUserId))
            {
                context.Fail("HUB user identity is required.");
                return false;
            }

            var verifier = scope.ServiceProvider.GetRequiredService<HubUserAccessVerifier>();
            var decision = await verifier.VerifyAsync(
                tenantAccess.HubProductInstanceId,
                hubUserId,
                email,
                context.HttpContext.RequestAborted);
            if (!decision.Active || decision.HubRole is null)
            {
                context.Fail("HUB user access is inactive.");
                return false;
            }

            ReplaceHubRoleClaim(context, decision.HubRole);
        }

        return await ValidateEmployeeAccessAsync(context, dbContext, tenantId);
    }

    private static void ReplaceHubRoleClaim(TokenValidatedContext context, string hubRole)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return;

        foreach (var claim in identity.FindAll("hub_role").ToList())
            identity.RemoveClaim(claim);
        identity.AddClaim(new Claim("hub_role", hubRole));
    }

    private static async Task<bool> ValidateEmployeeAccessAsync(
        TokenValidatedContext context,
        WorkBaseDbContext dbContext,
        Guid tenantId)
    {
        var employeeClaim = context.Principal?.FindFirstValue("employee_id");
        if (!string.IsNullOrWhiteSpace(employeeClaim))
        {
            if (!Guid.TryParse(employeeClaim, out var employeeId))
            {
                context.Fail("Employee context is invalid.");
                return false;
            }

            var employeeStatus = await dbContext.Set<Employee>()
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(employee => employee.Id == employeeId && employee.TenantId == tenantId)
                .Select(employee => (EmployeeStatus?)employee.Status)
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
            if (employeeStatus != EmployeeStatus.Active)
            {
                context.Fail("Employee access is inactive.");
                return false;
            }

            return true;
        }

        var subject = context.Principal?.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var keycloakUserId))
            return true;

        var linkedEmployeeStatus = await dbContext.Set<Employee>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(employee => employee.TenantId == tenantId && employee.UserId == keycloakUserId)
            .Select(employee => (EmployeeStatus?)employee.Status)
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
        if (linkedEmployeeStatus.HasValue && linkedEmployeeStatus != EmployeeStatus.Active)
        {
            context.Fail("Employee access is inactive.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Multi-realm only: for tokens from a DEDICATED tenant realm, the realm itself (issuer)
    /// is the authoritative source of tenant identity — the tenant_id user attribute becomes
    /// merely informational. This replaces any tenant_id claim in the token with the value
    /// mapped from the issuer (Tenant.KeycloakRealmName), so a mis-set or maliciously edited
    /// user attribute inside one realm can never grant access to another company's data.
    /// Shared-realm tokens (mapped to Guid.Empty in the cache) keep their attribute-based
    /// claim, since one realm hosts many tenants there.
    /// </summary>
    private static void OverrideTenantClaimFromIssuer(TokenValidatedContext context)
    {
        var identity = context.Principal?.Identity as ClaimsIdentity;
        if (identity is null)
            return;

        var issuer = context.SecurityToken switch
        {
            Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt => jwt.Issuer,
            System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt => jwt.Issuer,
            _ => null
        };
        if (issuer is null)
            return;

        var cache = context.HttpContext.RequestServices.GetRequiredService<TenantIssuerCache>();
        if (!cache.TryGetTenantId(issuer, out var tenantId) || tenantId == Guid.Empty)
            return; // shared realm or unknown issuer — leave the attribute-based claim as-is

        var existing = identity.FindFirst("tenant_id");
        if (existing is not null)
            identity.RemoveClaim(existing);

        identity.AddClaim(new Claim("tenant_id", tenantId.ToString()));
    }
}

