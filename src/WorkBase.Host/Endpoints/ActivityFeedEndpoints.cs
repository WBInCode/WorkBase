using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

public static class ActivityFeedEndpoints
{
    public static IEndpointRouteBuilder MapActivityFeedEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/activity")
            .WithTags("ActivityFeed")
            .RequireAuthorization();

        group.MapGet("/", async (WorkBaseDbContext db, HttpContext http,
            string? entityType = null, string? entityId = null, int page = 1, int pageSize = 50) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (pageSize > 200) pageSize = 200;

            var query = db.Set<AuditEntry>()
                .Where(a => a.TenantId == tenantId.Value);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(a => a.EntityId == entityId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivityFeedItemDto(
                    a.Id,
                    a.EntityType,
                    a.EntityId,
                    a.Action,
                    a.ChangedColumns,
                    a.UserId,
                    a.Timestamp))
                .ToListAsync();

            return Results.Ok(new ActivityFeedResponse(items, total, page, pageSize));
        })
        .WithName("GetActivityFeed")
        .WithSummary("Pobierz strumień aktywności");

        group.MapGet("/entity/{entityType}/{entityId}", async (
            string entityType, string entityId, WorkBaseDbContext db, HttpContext http, int page = 1, int pageSize = 50) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (pageSize > 200) pageSize = 200;

            var items = await db.Set<AuditEntry>()
                .Where(a => a.TenantId == tenantId.Value && a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new ActivityFeedItemDto(
                    a.Id,
                    a.EntityType,
                    a.EntityId,
                    a.Action,
                    a.ChangedColumns,
                    a.UserId,
                    a.Timestamp))
                .ToListAsync();

            return Results.Ok(items);
        })
        .WithName("GetEntityActivityFeed")
        .WithSummary("Pobierz historię zmian encji");

        return endpoints;
    }
}

public sealed record ActivityFeedItemDto(Guid Id, string EntityType, string EntityId, string Action, string? ChangedColumns, string? UserId, DateTime Timestamp);
public sealed record ActivityFeedResponse(IReadOnlyList<ActivityFeedItemDto> Items, int Total, int Page, int PageSize);
