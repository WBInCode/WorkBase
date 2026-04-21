using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sync").WithTags("OfflineSync").RequireAuthorization();

        group.MapPost("/push", async (SyncPushRequest req, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var entries = req.Operations.Select(op => new SyncQueueEntry
            {
                Id = Guid.NewGuid(), TenantId = tenantId.Value, UserId = userId,
                DeviceId = req.DeviceId, EntityType = op.EntityType, EntityId = op.EntityId,
                OperationType = op.OperationType, PayloadJson = op.PayloadJson,
                ClientTimestamp = op.ClientTimestamp, Status = "pending", CreatedAt = DateTime.UtcNow
            }).ToList();

            db.Set<SyncQueueEntry>().AddRange(entries);
            await db.SaveChangesAsync();
            return Results.Ok(new { Accepted = entries.Count });
        }).WithName("SyncPush").WithSummary("Prześlij operacje offline na serwer");

        group.MapGet("/pull", async (string deviceId, long? sinceTimestamp, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var query = db.Set<SyncQueueEntry>()
                .Where(e => e.TenantId == tenantId.Value && e.UserId == userId && e.Status == "synced");
            if (sinceTimestamp.HasValue)
                query = query.Where(e => e.ServerTimestamp > sinceTimestamp.Value);

            var items = await query.OrderBy(e => e.ServerTimestamp).Take(500).ToListAsync();
            return Results.Ok(items);
        }).WithName("SyncPull").WithSummary("Pobierz zsynchronizowane zmiany dla urządzenia");

        group.MapGet("/status", async (string deviceId, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            var userId = http.User.GetUserId();
            if (tenantId is null || userId is null) return Results.Forbid();

            var pending = await db.Set<SyncQueueEntry>()
                .CountAsync(e => e.DeviceId == deviceId && e.Status == "pending");
            var conflicts = await db.Set<SyncQueueEntry>()
                .CountAsync(e => e.DeviceId == deviceId && e.Status == "conflict");

            return Results.Ok(new { DeviceId = deviceId, Pending = pending, Conflicts = conflicts });
        }).WithName("SyncStatus").WithSummary("Status synchronizacji urządzenia");

        return endpoints;
    }
}

public sealed record SyncPushRequest(string DeviceId, List<SyncOperation> Operations);
public sealed record SyncOperation(string EntityType, string EntityId, string OperationType, string PayloadJson, long ClientTimestamp);
