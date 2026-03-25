using Hangfire.Dashboard;

namespace WorkBase.Infrastructure.BackgroundJobs;

/// <summary>
/// Allows Hangfire Dashboard access only from localhost.
/// Replace with role-based auth filter after T-E02 (JWT + RBAC).
/// </summary>
public sealed class HangfireLocalRequestOnlyFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.Connection.RemoteIpAddress is not null
            && (httpContext.Connection.RemoteIpAddress.Equals(httpContext.Connection.LocalIpAddress)
                || httpContext.Connection.RemoteIpAddress.ToString() == "::1"
                || httpContext.Connection.RemoteIpAddress.ToString() == "127.0.0.1");
    }
}
