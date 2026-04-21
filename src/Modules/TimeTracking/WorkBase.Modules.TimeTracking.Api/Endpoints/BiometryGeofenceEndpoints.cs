using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

public static class BiometryEndpoints
{
    public static IEndpointRouteBuilder MapBiometryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/biometry").WithTags("Biometry").RequireAuthorization();

        group.MapPost("/enroll", async (EnrollBiometricRequest req, ISender sender) =>
        {
            var result = await sender.Send(new EnrollBiometricCommand(req.EmployeeId, req.BiometricType, req.TemplateHash));
            return result.IsSuccess ? Results.Created($"/api/time/biometry/{result.Value}", result.Value) : result.ToHttpResult();
        }).WithName("EnrollBiometric").WithSummary("Rejestracja szablonu biometrycznego").RequirePermission("time.manage");

        group.MapPost("/clock-in", async (BiometricClockInRequest req, ISender sender) =>
        {
            var result = await sender.Send(new BiometricClockInCommand(req.TemplateHash, req.Latitude, req.Longitude));
            return result.IsSuccess ? Results.Ok(new { TimeEntryId = result.Value }) : result.ToHttpResult();
        }).WithName("BiometricClockIn").WithSummary("Rejestracja czasu biometrykiem");

        return endpoints;
    }
}

public static class GeofenceEndpoints
{
    public static IEndpointRouteBuilder MapGeofenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/geofence").WithTags("Geofence").RequireAuthorization();

        group.MapPost("/zones", async (CreateGeofenceZoneRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateGeofenceZoneCommand(req.Name, req.Latitude, req.Longitude,
                req.RadiusMeters, req.AutoClockIn, req.AutoClockOut));
            return result.IsSuccess ? Results.Created($"/api/time/geofence/zones/{result.Value}", result.Value) : result.ToHttpResult();
        }).WithName("CreateGeofenceZone").WithSummary("Utwórz strefę geofence").RequirePermission("time.manage");

        group.MapPost("/check-in", async (GeofenceCheckInRequest req, ISender sender) =>
        {
            var result = await sender.Send(new GeofenceCheckInCommand(req.EmployeeId, req.Latitude, req.Longitude));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToHttpResult();
        }).WithName("GeofenceCheckIn").WithSummary("Sprawdź geofence i automatyczne wejście/wyjście");

        return endpoints;
    }
}

public sealed record EnrollBiometricRequest(Guid EmployeeId, string BiometricType, string TemplateHash);
public sealed record BiometricClockInRequest(string TemplateHash, double? Latitude, double? Longitude);
public sealed record CreateGeofenceZoneRequest(string Name, double Latitude, double Longitude, int RadiusMeters, bool AutoClockIn, bool AutoClockOut);
public sealed record GeofenceCheckInRequest(Guid EmployeeId, double Latitude, double Longitude);
