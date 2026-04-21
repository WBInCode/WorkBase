using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Modules.TimeTracking.Api.Endpoints;

public static class QrTokenEndpoints
{
    public static IEndpointRouteBuilder MapQrTokenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/time/qr")
            .WithTags("TimeTracking - QR")
            .RequireAuthorization();

        group.MapPost("/generate", GenerateToken)
            .WithName("GenerateQrToken")
            .WithSummary("Generuj token QR do rejestracji czasu pracy")
            .Produces<QrTokenDto>(StatusCodes.Status201Created);

        group.MapPost("/verify", VerifyToken)
            .WithName("VerifyQrToken")
            .WithSummary("Zweryfikuj token QR i zarejestruj wejście")
            .RequirePermission("time.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GenerateToken(
        GenerateQrTokenRequest request,
        ISender sender)
    {
        var command = new GenerateQrTokenCommand(request.LocationId, request.TtlSeconds);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created("/api/time/qr", result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> VerifyToken(
        VerifyQrTokenRequest request,
        ISender sender)
    {
        var command = new VerifyQrTokenCommand(request.Token, request.EmployeeId);
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.Created($"/api/time/entries/{result.Value}", result.Value)
            : result.ToHttpResult();
    }
}

public sealed record GenerateQrTokenRequest(string? LocationId = null, int TtlSeconds = 30);
public sealed record VerifyQrTokenRequest(string Token, Guid EmployeeId);
