using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Organization.Application.Commands.Employees;

/// <param name="ZapraszajDoHuba">
/// Domyslnie <c>true</c>, zeby nie zmieniac zachowania dotychczasowych wywolan. Kreator
/// pierwszego startu przekazuje <c>false</c>: import 40 osob rozeslalby 40 zaproszen w Hubie,
/// czyli zapis w danych innego produktu, zanim wlasciciel zdazy cokolwiek sprawdzic.
/// Zapraszanie jest wtedy osobna, swiadoma decyzja.
/// </param>
public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string? EmployeeNumber,
    DateTime HireDate,
    bool ZapraszajDoHuba = true) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}
