namespace WorkBase.Shared.Storage;

public interface IFileStorage
{
    Task<string> UploadAsync(string bucket, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string bucket, string fileName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string bucket, string fileName, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string bucket, string fileName, CancellationToken cancellationToken = default);
}
