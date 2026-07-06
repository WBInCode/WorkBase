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

        if (multiRealmEnabled)
        {
            services.AddSingleton<TenantIssuerCache>();
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
}

