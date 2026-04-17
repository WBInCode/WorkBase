using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using WorkBase.Modules.Identity.Application.Contracts;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.Identity.Api.Endpoints;

public static class DataScopeEndpoints
{
    public static IEndpointRouteBuilder MapDataScopeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/iam/data-scopes")
            .WithTags("IAM – Data Scopes")
            .RequireAuthorization();

        group.MapGet("/", GetDataScopes)
            .WithName("GetDataScopes")
            .WithSummary("Pobierz zakresy danych")
            .RequirePermission("identity.view")
            .Produces<List<DataScopeDto>>();

        group.MapPost("/", CreateDataScope)
            .WithName("CreateDataScope")
            .WithSummary("Utwórz zakres danych")
            .RequirePermission("identity.edit")
            .Produces<Guid>(StatusCodes.Status201Created);

        group.MapPut("/{id:guid}", UpdateDataScope)
            .WithName("UpdateDataScope")
            .WithSummary("Zaktualizuj zakres danych")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent);

        group.MapDelete("/{id:guid}", DeleteDataScope)
            .WithName("DeleteDataScope")
            .WithSummary("Usuń zakres danych")
            .RequirePermission("identity.edit")
            .Produces(StatusCodes.Status204NoContent);

        return endpoints;
    }

    private static async Task<IResult> GetDataScopes(
        ClaimsPrincipal user, IDataScopeManagementService service, CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        var scopes = await service.GetByTenantAsync(tenantId.Value, ct);
        var dtos = scopes.Select(s =>
            new DataScopeDto(s.Id, s.RoleId, s.Module, s.ScopeLevel.ToString(), s.CustomFilter)).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateDataScope(
        CreateDataScopeRequest request, ClaimsPrincipal user,
        IDataScopeManagementService service, CancellationToken ct)
    {
        var tenantId = GetTenantId(user);
        if (tenantId is null) return Results.Forbid();

        if (!Enum.TryParse<DataScopeLevel>(request.ScopeLevel, true, out var level))
            return Results.BadRequest(new { Message = $"Nieprawidłowy poziom zakresu: {request.ScopeLevel}" });

        var id = await service.CreateAsync(tenantId.Value, request.RoleId, request.Module, level, request.CustomFilter, ct);
        return Results.Created($"/api/iam/data-scopes/{id}", id);
    }

    private static async Task<IResult> UpdateDataScope(
        Guid id, UpdateDataScopeRequest request,
        IDataScopeManagementService service, CancellationToken ct)
    {
        if (!Enum.TryParse<DataScopeLevel>(request.ScopeLevel, true, out var level))
            return Results.BadRequest(new { Message = $"Nieprawidłowy poziom zakresu: {request.ScopeLevel}" });

        var found = await service.UpdateAsync(id, level, request.CustomFilter, ct);
        return found ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> DeleteDataScope(
        Guid id, IDataScopeManagementService service, CancellationToken ct)
    {
        var found = await service.DeleteAsync(id, ct);
        return found ? Results.NoContent() : Results.NotFound();
    }

    private static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("tenant_id");
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public sealed record DataScopeDto(Guid Id, Guid RoleId, string Module, string ScopeLevel, string? CustomFilter);

public sealed record CreateDataScopeRequest(Guid RoleId, string Module, string ScopeLevel, string? CustomFilter = null);

public sealed record UpdateDataScopeRequest(string ScopeLevel, string? CustomFilter = null);
