namespace WorkBase.Modules.Integration.Application.Adapters;

public interface IFileStorageAdapter
{
    Task<List<FileMetadata>> ListFilesAsync(string folderId, string accessToken, CancellationToken ct);
    Task<FileMetadata> UploadFileAsync(string folderId, string fileName, Stream content, string accessToken, CancellationToken ct);
    Task<Stream> DownloadFileAsync(string fileId, string accessToken, CancellationToken ct);
    Task DeleteFileAsync(string fileId, string accessToken, CancellationToken ct);
    Task<FileMetadata> CreateFolderAsync(string parentId, string name, string accessToken, CancellationToken ct);
}

public interface IFileStorageAdapterFactory
{
    IFileStorageAdapter Create(string provider);
}

public record FileMetadata(string Id, string Name, string MimeType, long SizeBytes, DateTime ModifiedAt, bool IsFolder);
