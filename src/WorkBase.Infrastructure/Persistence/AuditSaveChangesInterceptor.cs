using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Persistence;

public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "Secret", "Token", "FileContent", "BlobContent", "AttachmentData"
    };

    private List<AuditEntryData>? _pendingAudits;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            _pendingAudits = CollectAuditEntries(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingAudits is { Count: > 0 } && eventData.Context is not null)
        {
            await PersistAuditEntriesAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditEntryData> CollectAuditEntries(DbContext context)
    {
        context.ChangeTracker.DetectChanges();

        var userId = httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        var tenantClaim = httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
        var tenantId = Guid.TryParse(tenantClaim, out var tid) ? tid : (Guid?)null;

        var audits = new List<AuditEntryData>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditEntry || entry.Entity is not IAuditable)
                continue;

            if (entry.State is EntityState.Detached or EntityState.Unchanged)
                continue;

            var auditData = new AuditEntryData
            {
                EntityType = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                UserId = userId,
                TenantId = tenantId
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditData.Action = "Created";
                    auditData.NewValues = GetPropertyValues(entry, e => e.CurrentValues);
                    break;

                case EntityState.Modified:
                    auditData.Action = "Modified";
                    auditData.OldValues = GetChangedOldValues(entry);
                    auditData.NewValues = GetChangedNewValues(entry);
                    auditData.ChangedColumns = GetChangedColumns(entry);
                    break;

                case EntityState.Deleted:
                    auditData.Action = "Deleted";
                    auditData.OldValues = GetPropertyValues(entry, e => e.OriginalValues);
                    break;
            }

            audits.Add(auditData);
        }

        return audits;
    }

    private async Task PersistAuditEntriesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var entries = _pendingAudits!.Select(a => AuditEntry.Create(
            a.EntityType,
            a.EntityId,
            a.Action,
            a.OldValues,
            a.NewValues,
            a.ChangedColumns,
            a.TenantId,
            a.UserId));

        context.Set<AuditEntry>().AddRange(entries);
        _pendingAudits = null;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties is null || keyProperties.Count == 0)
            return string.Empty;

        if (keyProperties.Count == 1)
            return entry.Property(keyProperties[0].Name).CurrentValue?.ToString() ?? string.Empty;

        var compositeKey = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? string.Empty);
        return string.Join('|', compositeKey);
    }

    private static string? GetPropertyValues(EntityEntry entry, Func<EntityEntry, PropertyValues> valuesSelector)
    {
        var values = valuesSelector(entry);
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (ExcludedProperties.Contains(property.Metadata.Name))
                continue;

            dict[property.Metadata.Name] = values[property.Metadata.Name];
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static string? GetChangedOldValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (ExcludedProperties.Contains(property.Metadata.Name))
                continue;

            dict[property.Metadata.Name] = property.OriginalValue;
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static string? GetChangedNewValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (ExcludedProperties.Contains(property.Metadata.Name))
                continue;

            dict[property.Metadata.Name] = property.CurrentValue;
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static string? GetChangedColumns(EntityEntry entry)
    {
        var columns = entry.Properties
            .Where(p => p.IsModified && !ExcludedProperties.Contains(p.Metadata.Name))
            .Select(p => p.Metadata.Name)
            .ToList();

        return columns.Count > 0 ? JsonSerializer.Serialize(columns, JsonOptions) : null;
    }

    private sealed class AuditEntryData
    {
        public string EntityType { get; set; } = default!;
        public string EntityId { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedColumns { get; set; }
        public string? UserId { get; set; }
        public Guid? TenantId { get; set; }
    }
}
