using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Organization.Application.Commands.Employees;
using WorkBase.Modules.Organization.Application.Dtos;
using WorkBase.Modules.Organization.Application.Queries.Employees;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

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
            .RequirePermission("org.create")
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("Pobierz listę pracowników (paginacja, filtry)")
            .RequirePermission("org.view")
            .Produces<PagedResultDto<EmployeeDto>>();

        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Pobierz szczegóły pracownika")
            .RequirePermission("org.view")
            .Produces<EmployeeDetailDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/assignment", AssignEmployee)
            .WithName("AssignEmployee")
            .WithSummary("Przypisz pracownika do jednostki i stanowiska")
            .RequirePermission("org.edit")
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/supervisor", SetSupervisor)
            .WithName("SetSupervisor")
            .WithSummary("Ustaw przełożonego pracownika")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithName("UpdateEmployee")
            .WithSummary("Zaktualizuj dane pracownika")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeactivateEmployee)
            .WithName("DeactivateEmployee")
            .WithSummary("Dezaktywuj pracownika")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/import", ImportEmployees)
            .WithName("ImportEmployees")
            .WithSummary("Importuj pracowników z CSV/JSON")
            .RequirePermission("org.create")
            .Produces<ImportEmployeesResult>(StatusCodes.Status200OK);

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

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        UpdateEmployeeRequest request,
        ISender sender)
    {
        var command = new UpdateEmployeeCommand(
            id, request.FirstName, request.LastName, request.Email, request.EmployeeNumber);
        var result = await sender.Send(command);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateEmployee(
        Guid id,
        ISender sender)
    {
        var result = await sender.Send(new DeactivateEmployeeCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> ImportEmployees(
        ImportEmployeesCommand command,
        ISender sender)
    {
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

public sealed record UpdateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string? EmployeeNumber);
