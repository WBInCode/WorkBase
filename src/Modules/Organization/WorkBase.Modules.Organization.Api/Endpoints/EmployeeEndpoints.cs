using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Contracts;
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

        group.MapPost("/{id:guid}/activate", ActivateEmployee)
            .WithName("ActivateEmployee")
            .WithSummary("Przywróć pracownika")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/import", ImportEmployees)
            .WithName("ImportEmployees")
            .WithSummary("Importuj pracowników z CSV/JSON")
            .RequirePermission("org.create")
            .Produces<ImportEmployeesResult>(StatusCodes.Status200OK);

        group.MapGet("/by-number/{employeeNumber}", GetEmployeeByNumber)
            .WithName("GetEmployeeByNumber")
            .WithSummary("Wyszukaj pracownika po numerze identyfikacyjnym (badge/PIN)")
            .RequireAuthorization()
            .Produces<EmployeeDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/link-user", LinkUser)
            .WithName("LinkUserToEmployee")
            .WithSummary("Powiąż konto Keycloak z pracownikiem")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/hourly-rate", SetHourlyRate)
            .WithName("SetEmployeeHourlyRate")
            .WithSummary("Ustaw stawkę godzinową pracownika")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/access-status", GetAccessStatus)
            .WithName("GetEmployeeAccessStatus")
            .WithSummary("Pobierz status dostępu pracownika do WorkBase przez HUB")
            .RequirePermission("org.view")
            .Produces<EmployeeAccessStatus>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/access-status/retry", RetryAccess)
            .WithName("RetryEmployeeAccess")
            .WithSummary("Ponów synchronizację dostępu pracownika z HUB")
            .RequirePermission("org.edit")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status409Conflict);

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
        ClaimsPrincipal user,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        CancellationToken ct,
        string? search = null,
        Guid? organizationUnitId = null,
        EmployeeStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new GetEmployeesQuery(search, organizationUnitId, status, page, pageSize);
        var result = await sender.Send(query, ct);
        if (!result.IsSuccess) return result.ToHttpResult();

        var strona = result.Value;
        var widoczne = await user.FilterAccessibleEmployeesAsync(
            strona.Items.Select(item => item.Id).ToList(),
            permissions, scopes, PodgladWynagrodzenZespolu, ModulWynagrodzen, ct);

        var pozycje = strona.Items
            .Select(item => widoczne.Contains(item.Id) ? item : item with { HourlyRate = null })
            .ToList();

        return Results.Ok(strona with { Items = pozycje });
    }

    /// <summary>Uprawnienie do ogladania cudzych stawek. Wlasna stawka nie wymaga niczego.</summary>
    private const string PodgladWynagrodzenZespolu = "payroll.view-team";

    /// <summary>
    /// Zakres liczymy wg modulu "org", nie "payroll" — i to jest celowe.
    /// „payroll" nie jest modulem z ModuleCatalog (uprawnienia payroll.* dopisano osobno),
    /// wiec nie ma dla niego ANI JEDNEGO wiersza w iam_data_scopes. Brak wierszy oznacza
    /// domyslny poziom Team, czyli sam pytajacy i jego bezposredni podwladni — administrator
    /// zobaczylby wtedy puste stawki wiekszosci firmy i ekran plac pokazalby zera.
    /// Modul "org" ma zakresy nadane wszystkim rolom (Organization dla Admin/HR,
    /// Department dla Kierownika, Own dla Pracownika) i dokladnie tak ma dzialac widocznosc
    /// stawek. Uprawnienie payroll.view-team nadal decyduje, czy w ogole pytamy o zakres.
    /// </summary>
    private const string ModulWynagrodzen = "org";

    /// <summary>
    /// Zeruje stawke godzinowa, jesli pytajacy nie ma prawa jej widziec.
    /// </summary>
    /// <remarks>
    /// <see cref="EmployeeDto"/> i <see cref="EmployeeDetailDto"/> niosa HourlyRate, a wszystkie
    /// trzy odczyty pracownika (lista, karta, wyszukanie po numerze) oddawaly ja kazdemu
    /// z uprawnieniem org.view — czyli KAZDEMU zalogowanemu, bo org.view ma rola "Pracownik".
    /// Model uprawnien od poczatku rozroznia payroll.view (wlasne rozliczenie) od
    /// payroll.view-team (zespol i firma); ekran /payroll to respektowal, a te endpointy nie.
    /// </remarks>
    private static async Task<bool> MozeWidziecStawkeAsync(
        ClaimsPrincipal user,
        Guid employeeId,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        CancellationToken ct)
        => await user.CanAccessEmployeeAsync(
            employeeId, permissions, scopes, PodgladWynagrodzenZespolu, ModulWynagrodzen, ct);

    private static async Task<IResult> GetEmployeeById(
        Guid id,
        ClaimsPrincipal user,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployeeByIdQuery(id), ct);
        if (!result.IsSuccess) return result.ToHttpResult();

        var pracownik = result.Value;
        if (await MozeWidziecStawkeAsync(user, pracownik.Id, permissions, scopes, ct))
            return Results.Ok(pracownik);

        return Results.Ok(pracownik with { HourlyRate = null });
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

    private static async Task<IResult> ActivateEmployee(
        Guid id,
        ISender sender)
    {
        var result = await sender.Send(new ActivateEmployeeCommand(id));
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> ImportEmployees(
        ImportEmployeesCommand command,
        ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetEmployeeByNumber(
        string employeeNumber,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        ISender sender,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId) || !Guid.TryParse(tenantId, out var tid))
            return Results.Forbid();

        var result = await sender.Send(new GetEmployeeByNumberQuery(tid, employeeNumber), ct);
        if (!result.IsSuccess) return result.ToHttpResult();

        var pracownik = result.Value;
        if (await MozeWidziecStawkeAsync(httpContext.User, pracownik.Id, permissions, scopes, ct))
            return Results.Ok(pracownik);

        return Results.Ok(pracownik with { HourlyRate = null });
    }

    private static async Task<IResult> GetAccessStatus(
        Guid id,
        HttpContext httpContext,
        IEmployeeAccessStatusService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(httpContext, out var tenantId))
            return Results.Forbid();

        var status = await service.GetAsync(tenantId, id, cancellationToken);
        return status is null ? Results.NotFound() : Results.Ok(status);
    }

    private static async Task<IResult> RetryAccess(
        Guid id,
        HttpContext httpContext,
        IEmployeeAccessStatusService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(httpContext, out var tenantId))
            return Results.Forbid();

        var queued = await service.RetryAsync(tenantId, id, cancellationToken);
        return queued
            ? Results.NoContent()
            : Results.Conflict(new { Message = "Brak nieudanej operacji dostępu do ponowienia." });
    }

    private static bool TryGetTenantId(HttpContext httpContext, out Guid tenantId)
    {
        var claim = httpContext.User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out tenantId);
    }

    private static async Task<IResult> LinkUser(
        Guid id,
        LinkUserRequest request,
        ISender sender)
    {
        var command = new LinkUserCommand(id, request.UserId);
        var result = await sender.Send(command);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
    }

    private static async Task<IResult> SetHourlyRate(
        Guid id,
        SetHourlyRateRequest request,
        ISender sender)
    {
        var command = new SetEmployeeHourlyRateCommand(id, request.HourlyRate);
        var result = await sender.Send(command);
        return result.IsSuccess ? Results.NoContent() : result.ToHttpResult();
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

public sealed record LinkUserRequest(
    Guid UserId);

public sealed record SetHourlyRateRequest(
    decimal? HourlyRate);
