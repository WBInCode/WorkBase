using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = requireHttps;

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
