using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Notification.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Notification.Api;

public sealed class NotificationEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapNotificationEndpoints();
        return endpoints;
    }
}
