using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Employees;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.Employees;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/org/employees")
            .WithTags("Employees")
            .RequireAuthorization();

        group.MapPost("/", CreateEmployee)
            .WithName("CreateEmployee")
            .WithSummary("Utwórz pracownika")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("Pobierz listę pracowników (paginacja, filtry)")
            .Produces<PagedResultDto<EmployeeDto>>();

        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Pobierz szczegóły pracownika")
            .Produces<EmployeeDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/assignment", AssignEmployee)
            .WithName("AssignEmployee")
            .WithSummary("Przypisz pracownika do jednostki i stanowiska")
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/supervisor", SetSupervisor)
            .WithName("SetSupervisor")
            .WithSummary("Ustaw przełożonego pracownika")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateEmployee(
        CreateEmployeeCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);

        return result.IsSuccess
            ? Results.CreatedAtRoute("GetEmployeeById", new { id = result.Value }, result.Value)
            : result.ToHttpResult();
    }

    private static async Task<IResult> GetEmployees(
        ISender sender,
        string? search = null,
        Guid? organizationUnitId = null,
        EmployeeStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new GetEmployeesQuery(search, organizationUnitId, status, page, pageSize);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetEmployeeById(
        Guid id,
        ISender sender)
    {
        var result = await sender.Send(new GetEmployeeByIdQuery(id));
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignEmployee(
        Guid id,
        AssignEmployeeRequest request,
        ISender sender)
    {
        var command = new AssignEmployeeCommand(
            id,
            request.OrganizationUnitId,
            request.PositionId,
            request.IsPrimary,
            request.StartDate);

        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SetSupervisor(
        Guid id,
        SetSupervisorRequest request,
        ISender sender)
    {
        var command = new SetSupervisorCommand(id, request.SupervisorEmployeeId);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}

public sealed record AssignEmployeeRequest(
    Guid OrganizationUnitId,
    Guid PositionId,
    bool IsPrimary,
    DateTime StartDate);

public sealed record SetSupervisorRequest(
    Guid SupervisorEmployeeId);
