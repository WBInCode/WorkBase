using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.Tenants;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Organization.Api.Endpoints;

/// <summary>
/// Platform-operator "companies" panel endpoints (docs/05-module-licensing-architecture.md
/// step 5). Cross-tenant by design — gated by RequirePlatformOperator(), not RequirePermission().
/// </summary>
public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/tenants")
            .WithTags("Platform – Tenants")
            .RequireAuthorization();

        group.MapGet("/", GetTenants)
            .WithName("GetTenants")
            .WithSummary("Lista wszystkich firm (tenantów) w systemie — tylko dla operatora platformy")
            .RequirePlatformOperator()
            .Produces<List<TenantSummaryDto>>();

        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Onboarduj nową firmę (tenant) — tworzy Tenant + role/uprawnienia + konto admina firmy")
            .RequirePlatformOperator()
            .Produces<CreateTenantResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetTenants(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantsQuery(), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateTenant(
        CreateTenantRequest request, ITenantProvisioningService provisioningService, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug)
            || string.IsNullOrWhiteSpace(request.AdminEmail))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Nazwa, slug i e-mail administratora są wymagane.");
        }

        try
        {
            var result = await provisioningService.CreateTenantAsync(
                request.Name, request.Slug,
                request.AdminEmail, request.AdminFirstName ?? "Admin", request.AdminLastName ?? request.Name,
                ct);

            var response = new CreateTenantResponse(
                result.TenantId,
                result.AdminEmail,
                result.AdminTemporaryPassword,
                KeycloakAccountCreated: result.AdminTemporaryPassword is not null,
                result.KeycloakRealmName);

            return Results.Created($"/api/org/tenants/{result.TenantId}", response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: ex.Message);
        }
    }
}

public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string AdminEmail,
    string? AdminFirstName,
    string? AdminLastName);

/// <summary>
/// AdminTemporaryPassword is returned exactly once, straight from this response — it is never
/// stored anywhere (Keycloak marks it temporary and forces a change at first login).
/// KeycloakRealmName is non-null in multi-realm mode: the company's users must log in via
/// this dedicated realm (frontend: /?realm={KeycloakRealmName}).
/// </summary>
public sealed record CreateTenantResponse(
    Guid TenantId,
    string AdminEmail,
    string? AdminTemporaryPassword,
    bool KeycloakAccountCreated,
    string? KeycloakRealmName);

