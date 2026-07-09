using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Documents.Application.Contracts;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Domain;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Tenant-configurable upload restrictions for the Documents module (max file size, allowed
/// extensions) — docs/AUDIT-KNOWLEDGE-MAP.md (module parametrization). Stored via
/// <see cref="ITenantConfigService"/> under the "document_upload" key, the same key
/// <c>UploadDocumentHandler</c> already reads, so saving here immediately affects uploads.
/// </summary>
public static class DocumentSettingsEndpoints
{
    private const string ConfigKey = "document_upload";
    private const long MinFileSizeBytes = 1024; // 1 KB
    private const long MaxFileSizeBytesLimit = 500 * 1024 * 1024; // 500 MB hard ceiling

    public static IEndpointRouteBuilder MapDocumentSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents/settings").WithTags("DocumentSettings").RequireAuthorization();

        group.MapGet("/", async (ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var settings = await config.GetAsync<DocumentUploadSettings>(tenantId.Value, ConfigKey)
                ?? new DocumentUploadSettings();
            return Results.Ok(settings);
        }).WithName("GetDocumentSettings").WithSummary("Pobierz ustawienia uploadu dokumentów");

        group.MapPut("/", async (DocumentUploadSettings request, ITenantConfigService config, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            if (request.MaxFileSizeBytes < MinFileSizeBytes || request.MaxFileSizeBytes > MaxFileSizeBytesLimit)
                return Results.BadRequest(new { message = $"Maksymalny rozmiar pliku musi mieścić się między 1 KB a {MaxFileSizeBytesLimit / (1024 * 1024)} MB." });

            var extensions = (request.AllowedExtensions ?? []).Select(e => e.Trim().ToLowerInvariant()).Where(e => e.Length > 0).Distinct().ToList();
            if (extensions.Count == 0)
                return Results.BadRequest(new { message = "Wskaż przynajmniej jedno dozwolone rozszerzenie pliku." });
            if (extensions.Any(e => !e.StartsWith('.') || e.Any(c => char.IsWhiteSpace(c))))
                return Results.BadRequest(new { message = "Rozszerzenia muszą zaczynać się od kropki i nie zawierać spacji, np. '.pdf'." });

            request.AllowedExtensions = extensions;
            await config.SetAsync(tenantId.Value, ConfigKey, request);
            return Results.NoContent();
        }).WithName("UpdateDocumentSettings").WithSummary("Zapisz ustawienia uploadu dokumentów").RequirePermission("config.manage");

        return endpoints;
    }
}
