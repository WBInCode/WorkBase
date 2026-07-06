using Hangfire.Dashboard;

namespace WorkBase.Infrastructure.BackgroundJobs;

/// <summary>
/// Allows Hangfire Dashboard access only to authenticated users holding the
/// "workbase-admin" role (Keycloak realm role, mapped to the "roles" claim).
/// Replaces <see cref="HangfireLocalRequestOnlyFilter"/>, which is unsafe behind
/// a reverse proxy where all traffic can appear to originate from localhost.
/// </summary>
public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const string AdminRole = "workbase-admin";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;

        return user.Identity is { IsAuthenticated: true } && user.IsInRole(AdminRole);
    }
}
