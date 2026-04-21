using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

public sealed class OneDriveAdapter : IFileStorageAdapter
{
    private static readonly HttpClient Http = new();
    private const string GraphBase = "https://graph.microsoft.com/v1.0/me/drive";

    public async Task<List<FileMetadata>> ListFilesAsync(string folderId, string accessToken, CancellationToken ct)
    {
        var url = folderId == "root"
            ? $"{GraphBase}/root/children?$select=id,name,file,folder,size,lastModifiedDateTime"
            : $"{GraphBase}/items/{folderId}/children?$select=id,name,file,folder,size,lastModifiedDateTime";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("value").EnumerateArray().Select(f => new FileMetadata(
            f.GetProperty("id").GetString()!, f.GetProperty("name").GetString()!,
            f.TryGetProperty("file", out var file) ? file.GetProperty("mimeType").GetString()! : "folder",
            f.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
            f.GetProperty("lastModifiedDateTime").GetDateTime(),
            f.TryGetProperty("folder", out _)
        )).ToList();
    }

    public async Task<FileMetadata> UploadFileAsync(string folderId, string fileName, Stream content, string accessToken, CancellationToken ct)
    {
        var url = folderId == "root"
            ? $"{GraphBase}/root:/{Uri.EscapeDataString(fileName)}:/content"
            : $"{GraphBase}/items/{folderId}:/{Uri.EscapeDataString(fileName)}:/content";
        using var req = new HttpRequestMessage(HttpMethod.Put, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = new StreamContent(content);
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var f = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new FileMetadata(f.GetProperty("id").GetString()!, f.GetProperty("name").GetString()!,
            f.TryGetProperty("file", out var file) ? file.GetProperty("mimeType").GetString()! : "application/octet-stream",
            f.TryGetProperty("size", out var s) ? s.GetInt64() : 0,
            f.GetProperty("lastModifiedDateTime").GetDateTime(), false);
    }

    public async Task<Stream> DownloadFileAsync(string fileId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{GraphBase}/items/{fileId}/content");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(ct);
    }

    public async Task DeleteFileAsync(string fileId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"{GraphBase}/items/{fileId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<FileMetadata> CreateFolderAsync(string parentId, string name, string accessToken, CancellationToken ct)
    {
        var url = parentId == "root"
            ? $"{GraphBase}/root/children"
            : $"{GraphBase}/items/{parentId}/children";
        var body = new { name, folder = new { }, conflictBehavior = "rename" };
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(body);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var f = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new FileMetadata(f.GetProperty("id").GetString()!, name, "folder", 0,
            f.GetProperty("lastModifiedDateTime").GetDateTime(), true);
    }
}
