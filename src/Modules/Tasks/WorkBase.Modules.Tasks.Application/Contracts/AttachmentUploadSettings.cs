namespace WorkBase.Modules.Tasks.Application.Contracts;

/// <summary>
/// Załączniki zadań podlegają tej samej polityce co dokumenty — jeden klucz konfiguracji
/// najemcy (`document_upload`), żeby limit i lista rozszerzeń nie rozjechały się między modułami.
/// </summary>
public sealed class AttachmentUploadSettings
{
    public const string TenantConfigKey = "document_upload";

    public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;

    public List<string> AllowedExtensions { get; set; } =
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".png", ".jpg", ".jpeg", ".gif", ".txt", ".csv", ".zip",
    ];
}
