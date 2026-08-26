using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Commands.Employees;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.Organization;

/// <summary>
/// Dodanie pracownika kolejkuje zaproszenie do Huba, czyli zapis w danych INNEGO produktu.
/// Kreator pierwszego startu importuje cala firme naraz, wiec bez tej flagi wlasciciel
/// rozeslalby kilkadziesiat zaproszen, zanim zdazy cokolwiek sprawdzic.
///
/// Domyslka musi zostac przy „zapraszaj", bo tego oczekuje panel administratora — test
/// pilnuje obu stron tej decyzji, nie tylko nowej.
/// </summary>
public class ZapraszanieDoHubaTests
{
    private static readonly Guid Firma = Guid.Parse("30000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Import_bez_zapraszania_nie_kolejkuje_ani_jednego_zaproszenia()
    {
        var (repozytorium, kolejka) = Atrapy();
        var handler = new ImportEmployeesHandler(repozytorium, kolejka);

        var wynik = await handler.Handle(
            new ImportEmployeesCommand(Wiersze(3), ZapraszajDoHuba: false) { TenantId = Firma },
            CancellationToken.None);

        Assert.True(wynik.IsSuccess);
        Assert.Equal(3, wynik.Value.Imported);
        await kolejka.DidNotReceiveWithAnyArgs()
            .QueueInvitationAsync(default!, default);
    }

    [Fact]
    public async Task Import_domyslnie_zaprasza_zeby_nie_zmienic_zachowania_panelu()
    {
        var (repozytorium, kolejka) = Atrapy();
        var handler = new ImportEmployeesHandler(repozytorium, kolejka);

        var wynik = await handler.Handle(
            new ImportEmployeesCommand(Wiersze(3)) { TenantId = Firma },
            CancellationToken.None);

        Assert.True(wynik.IsSuccess);
        await kolejka.Received(3).QueueInvitationAsync(
            Arg.Any<EmployeeAccessInvitationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pojedynczy_pracownik_bez_zapraszania_tez_nie_trafia_do_kolejki()
    {
        var (repozytorium, kolejka) = Atrapy();
        var handler = new CreateEmployeeHandler(repozytorium, kolejka);

        var wynik = await handler.Handle(
            new CreateEmployeeCommand("Anna", "Kowalska", "anna@example.com", null, DateTime.UtcNow.Date,
                ZapraszajDoHuba: false)
            { TenantId = Firma },
            CancellationToken.None);

        Assert.True(wynik.IsSuccess);
        await kolejka.DidNotReceiveWithAnyArgs().QueueInvitationAsync(default!, default);
    }

    private static (IEmployeeRepository, IEmployeeAccessProvisioningQueue) Atrapy()
    {
        var repozytorium = Substitute.For<IEmployeeRepository>();
        repozytorium
            .EmailExistsInTenantAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        repozytorium
            .AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return (repozytorium, Substitute.For<IEmployeeAccessProvisioningQueue>());
    }

    private static List<ImportEmployeeRow> Wiersze(int ile) =>
        Enumerable.Range(1, ile)
            .Select(i => new ImportEmployeeRow(
                $"Imie{i}", $"Nazwisko{i}", $"osoba{i}@example.com", null, DateTime.UtcNow.Date))
            .ToList();
}
