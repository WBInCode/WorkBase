using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WorkBase.Infrastructure.Auth.MultiRealm;

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

                            if (context.Principal is not null)
                            {
                                await UserProvisioningService.OnTokenValidatedAsync(
                                    context.HttpContext.RequestServices,
                                    context.Principal);
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

                            if (context.Principal is not null)
                            {
                                await UserProvisioningService.OnTokenValidatedAsync(
                                    context.HttpContext.RequestServices,
                                    context.Principal);
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

