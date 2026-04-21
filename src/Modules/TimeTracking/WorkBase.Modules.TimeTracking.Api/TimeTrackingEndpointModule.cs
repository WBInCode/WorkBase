using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Api.Endpoints;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Api;

public sealed class TimeTrackingEndpointModule : IEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTimeEntryEndpoints();
        endpoints.MapQrTokenEndpoints();
        endpoints.MapScheduleEndpoints();
        endpoints.MapAnomalyEndpoints();
        endpoints.MapTimeCorrectionEndpoints();
        endpoints.MapNfcEndpoints();
        endpoints.MapBiometryEndpoints();
        endpoints.MapGeofenceEndpoints();
        return endpoints;
    }
}
