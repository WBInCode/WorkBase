using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WorkBase.Modules.Organization.Application.Commands.Positions;
using WorkBase.Modules.Organization.Application.Services;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Organization.Infrastructure.Repositories;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Ponowne zastosowanie polityki stanowisk do przypisan sprzed jej wprowadzenia.
/// </summary>
/// <remarks>
/// Audyt produkcji: stanowiska byly poprawnie oznaczone jako kierownicze i mialy role domyslne,
/// ludzie byli na nie przypisani, ale tabela relacji przelozonych byla PUSTA, a role domyslne
/// nienadane. Polityka dziala tylko w momencie przypisania, a przypisania powstaly wczesniej.
/// Skutek: nikt nie mial przelozonego, wiec nikt nie mogl zlozyc wniosku urlopowego
/// (zero wnioskow w bazie), a kierownicy nie mieli roli pozwalajacej zarzadzac czasem zespolu.
/// </remarks>
public sealed class ReapplyPositionPolicyTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task Nadaje_przelozonego_i_role_dla_istniejacych_przypisan()
    {
        await using var db = CreateDbContext();

        var rolaKierownika = Guid.NewGuid();
        var jednostka = Guid.NewGuid();

        var stanowiskoKierownik = Position.Create(TenantId, "Kierownik", null, rolaKierownika, isManagerial: true);
        var stanowiskoPracownik = Position.Create(TenantId, "Pracownik", null, null, isManagerial: false);
        var kierownik = Employee.Create(TenantId, "Anna", "Kierowniczka", "anna@example.com", null, DateTime.UtcNow.Date, Guid.NewGuid());
        var pracownik = Employee.Create(TenantId, "Jan", "Pracownik", "jan@example.com", null, DateTime.UtcNow.Date, Guid.NewGuid());
        db.AddRange(stanowiskoKierownik, stanowiskoPracownik, kierownik, pracownik);
        await db.SaveChangesAsync();

        db.AddRange(
            EmployeeAssignment.Create(TenantId, kierownik.Id, jednostka, stanowiskoKierownik.Id, isPrimary: true, DateTime.UtcNow.Date),
            EmployeeAssignment.Create(TenantId, pracownik.Id, jednostka, stanowiskoPracownik.Id, isPrimary: true, DateTime.UtcNow.Date));
        await db.SaveChangesAsync();

        Assert.Empty(await db.Set<SupervisorRelation>().ToListAsync());

        var role = Substitute.For<IRoleManagementService>();
        var handler = UtworzHandler(db, role);

        var wynik = await handler.Handle(new ReapplyPositionPolicyCommand { TenantId = TenantId }, default);
        await db.SaveChangesAsync();

        Assert.True(wynik.IsSuccess);
        Assert.Equal(2, wynik.Value.PrzetworzonychPrzypisan);

        var relacje = await db.Set<SupervisorRelation>().ToListAsync();
        var relacja = Assert.Single(relacje);
        Assert.Equal(kierownik.Id, relacja.SupervisorEmployeeId);
        Assert.Equal(pracownik.Id, relacja.SubordinateEmployeeId);

        await role.Received().ApplyPositionRoleAsync(
            kierownik.UserId!.Value, TenantId, rolaKierownika, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Powtorne_uruchomienie_nie_dubluje_relacji()
    {
        await using var db = CreateDbContext();

        var jednostka = Guid.NewGuid();
        var stanowiskoKierownik = Position.Create(TenantId, "Kierownik", null, null, isManagerial: true);
        var stanowiskoPracownik = Position.Create(TenantId, "Pracownik", null, null, isManagerial: false);
        var kierownik = Employee.Create(TenantId, "Anna", "Kierowniczka", "anna2@example.com", null, DateTime.UtcNow.Date, Guid.NewGuid());
        var pracownik = Employee.Create(TenantId, "Jan", "Pracownik", "jan2@example.com", null, DateTime.UtcNow.Date, Guid.NewGuid());
        db.AddRange(stanowiskoKierownik, stanowiskoPracownik, kierownik, pracownik);
        await db.SaveChangesAsync();
        db.AddRange(
            EmployeeAssignment.Create(TenantId, kierownik.Id, jednostka, stanowiskoKierownik.Id, isPrimary: true, DateTime.UtcNow.Date),
            EmployeeAssignment.Create(TenantId, pracownik.Id, jednostka, stanowiskoPracownik.Id, isPrimary: true, DateTime.UtcNow.Date));
        await db.SaveChangesAsync();

        var handler = UtworzHandler(db, Substitute.For<IRoleManagementService>());

        await handler.Handle(new ReapplyPositionPolicyCommand { TenantId = TenantId }, default);
        await db.SaveChangesAsync();
        var poPierwszym = await db.Set<SupervisorRelation>().CountAsync();

        await handler.Handle(new ReapplyPositionPolicyCommand { TenantId = TenantId }, default);
        await db.SaveChangesAsync();

        Assert.Equal(poPierwszym, await db.Set<SupervisorRelation>().CountAsync());
    }

    private static ReapplyPositionPolicyHandler UtworzHandler(WorkBaseDbContext db, IRoleManagementService role)
    {
        var employees = new EmployeeRepository(db);
        var assignments = new EmployeeAssignmentRepository(db);
        var positions = new PositionRepository(db);
        var supervisors = new SupervisorRelationRepository(db);

        return new ReapplyPositionPolicyHandler(
            assignments, employees, positions,
            new PositionAssignmentPolicy(employees, assignments, positions, supervisors, role));
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"reapply-position-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}
