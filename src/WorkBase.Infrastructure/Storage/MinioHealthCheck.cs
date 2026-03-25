using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;

namespace WorkBase.Infrastructure.Storage;

internal sealed class MinioHealthCheck : IHealthCheck
{
    private readonly IMinioClient _client;

    public MinioHealthCheck(IMinioClient client)
    {
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ListBuckets is the lightest operation to verify connectivity
            await _client.ListBucketsAsync(cancellationToken);
            return HealthCheckResult.Healthy("MinIO is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO is unreachable.", ex);
        }
    }
}
