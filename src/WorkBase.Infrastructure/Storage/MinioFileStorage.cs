using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using WorkBase.Shared.Storage;

namespace WorkBase.Infrastructure.Storage;

internal sealed class MinioFileStorage : IFileStorage
{
    private readonly IMinioClient _client;
    private readonly ILogger<MinioFileStorage> _logger;

    public MinioFileStorage(IMinioClient client, ILogger<MinioFileStorage> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        string bucket, string fileName, Stream content, string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(bucket, cancellationToken);

        var args = new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(args, cancellationToken);

        _logger.LogInformation("Uploaded {FileName} to bucket {Bucket}", fileName, bucket);

        return fileName;
    }

    public async Task<Stream> DownloadAsync(
        string bucket, string fileName,
        CancellationToken cancellationToken = default)
    {
        var memoryStream = new MemoryStream();

        var args = new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await _client.GetObjectAsync(args, cancellationToken);

        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(
        string bucket, string fileName,
        CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(bucket)
            .WithObject(fileName);

        await _client.RemoveObjectAsync(args, cancellationToken);

        _logger.LogInformation("Deleted {FileName} from bucket {Bucket}", fileName, bucket);
    }

    public async Task<bool> ExistsAsync(
        string bucket, string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucket)
                .WithObject(fileName);

            await _client.StatObjectAsync(args, cancellationToken);
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            return false;
        }
    }

    private async Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(bucket);
        if (!await _client.BucketExistsAsync(existsArgs, cancellationToken))
        {
            var makeArgs = new MakeBucketArgs().WithBucket(bucket);
            await _client.MakeBucketAsync(makeArgs, cancellationToken);
            _logger.LogInformation("Created bucket {Bucket}", bucket);
        }
    }
}
