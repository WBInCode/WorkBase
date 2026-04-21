using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

public sealed class GoogleDriveAdapter : IFileStorageAdapter
{
    private static readonly HttpClient Http = new();
    private const string ApiBase = "https://www.googleapis.com/drive/v3";

    public async Task<List<FileMetadata>> ListFilesAsync(string folderId, string accessToken, CancellationToken ct)
    {
        var query = $"'{folderId}' in parents and trashed=false";
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/files?q={Uri.EscapeDataString(query)}&fields=files(id,name,mimeType,size,modifiedTime)");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("files").EnumerateArray().Select(f => new FileMetadata(
            f.GetProperty("id").GetString()!,
            f.GetProperty("name").GetString()!,
            f.GetProperty("mimeType").GetString()!,
            f.TryGetProperty("size", out var s) ? long.Parse(s.GetString()!) : 0,
            f.GetProperty("modifiedTime").GetDateTime(),
            f.GetProperty("mimeType").GetString() == "application/vnd.google-apps.folder"
        )).ToList();
    }

    public async Task<FileMetadata> UploadFileAsync(string folderId, string fileName, Stream content, string accessToken, CancellationToken ct)
    {
        var metadata = JsonSerializer.Serialize(new { name = fileName, parents = new[] { folderId } });
        using var multipart = new MultipartContent("related");
        var metaPart = new StringContent(metadata, System.Text.Encoding.UTF8, "application/json");
        var filePart = new StreamContent(content);
        filePart.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        multipart.Add(metaPart);
        multipart.Add(filePart);

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart&fields=id,name,mimeType,size,modifiedTime");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = multipart;
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var f = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new FileMetadata(
            f.GetProperty("id").GetString()!, f.GetProperty("name").GetString()!,
            f.GetProperty("mimeType").GetString()!,
            f.TryGetProperty("size", out var s) ? long.Parse(s.GetString()!) : 0,
            f.GetProperty("modifiedTime").GetDateTime(), false);
    }

    public async Task<Stream> DownloadFileAsync(string fileId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/files/{fileId}?alt=media");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync(ct);
    }

    public async Task DeleteFileAsync(string fileId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"{ApiBase}/files/{fileId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<FileMetadata> CreateFolderAsync(string parentId, string name, string accessToken, CancellationToken ct)
    {
        var body = new { name, mimeType = "application/vnd.google-apps.folder", parents = new[] { parentId } };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/files?fields=id,name,mimeType,modifiedTime");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(body);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var f = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new FileMetadata(f.GetProperty("id").GetString()!, name, "application/vnd.google-apps.folder", 0, f.GetProperty("modifiedTime").GetDateTime(), true);
    }
}
