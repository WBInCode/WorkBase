using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

public static class SavedViewEndpoints
{
    public static IEndpointRouteBuilder MapSavedViewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/views")
            .WithTags("SavedViews")
            .RequireAuthorization();

        group.MapGet("/{entityType}", async (string entityType, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var views = await db.Set<SavedView>()
                .Where(v => v.TenantId == tenantId.Value && v.EntityType == entityType
                    && (v.UserId == userId || v.IsShared))
                .OrderByDescending(v => v.IsDefault)
                .ThenBy(v => v.Name)
                .ToListAsync();
            return Results.Ok(views);
        })
        .WithName("GetSavedViews")
        .WithSummary("Pobierz zapisane widoki dla typu encji");

        group.MapPost("/", async (CreateSavedViewRequest request, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var view = new SavedView
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                UserId = userId,
                EntityType = request.EntityType,
                Name = request.Name,
                FiltersJson = request.FiltersJson,
                SortJson = request.SortJson,
                ColumnsJson = request.ColumnsJson,
                IsDefault = request.IsDefault,
                IsShared = request.IsShared,
                CreatedAt = DateTime.UtcNow
            };
            db.Set<SavedView>().Add(view);
            await db.SaveChangesAsync();
            return Results.Created($"/api/views/{view.Id}", view);
        })
        .WithName("CreateSavedView")
        .WithSummary("Utwórz zapisany widok");

        group.MapPut("/{id:guid}", async (Guid id, UpdateSavedViewRequest request, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var view = await db.Set<SavedView>()
                .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId.Value && v.UserId == userId);
            if (view is null) return Results.NotFound();

            view.Name = request.Name;
            view.FiltersJson = request.FiltersJson;
            view.SortJson = request.SortJson;
            view.ColumnsJson = request.ColumnsJson;
            view.IsDefault = request.IsDefault;
            view.IsShared = request.IsShared;
            view.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(view);
        })
        .WithName("UpdateSavedView")
        .WithSummary("Aktualizuj zapisany widok");

        group.MapDelete("/{id:guid}", async (Guid id, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var view = await db.Set<SavedView>()
                .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId.Value && v.UserId == userId);
            if (view is null) return Results.NotFound();

            db.Set<SavedView>().Remove(view);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteSavedView")
        .WithSummary("Usuń zapisany widok");

        return endpoints;
    }
}

public sealed record CreateSavedViewRequest(string EntityType, string Name, string FiltersJson, string SortJson, string? ColumnsJson, bool IsDefault, bool IsShared);
public sealed record UpdateSavedViewRequest(string Name, string FiltersJson, string SortJson, string? ColumnsJson, bool IsDefault, bool IsShared);
