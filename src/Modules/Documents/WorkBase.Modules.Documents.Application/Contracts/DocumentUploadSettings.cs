namespace WorkBase.Modules.Documents.Application.Contracts;

/// <summary>
/// Tenant-configurable upload restrictions for the Documents module (max size, allowed
/// extensions) — see docs/AUDIT-KNOWLEDGE-MAP.md (module parametrization). Stored via
/// ITenantConfigService under the "document_upload" key. Defaults are applied when no
/// tenant-specific row exists yet.
/// </summary>
public sealed class DocumentUploadSettings
{
    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024; // 25 MB

    public List<string> AllowedExtensions { get; set; } =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".png", ".jpg", ".jpeg", ".gif", ".txt", ".csv", ".zip",
    ];
}
