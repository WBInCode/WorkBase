using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Entities;
using Xunit;
using TaskStatusEntity = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Nowa firma musi dostac z provisioningu komplet slownikow potrzebnych do pracy.
/// </summary>
/// <remarks>
/// Audyt produkcji: firma zalozona przez Hub dostawala wylacznie role i korzen struktury.
/// Typow urlopu 0, statusow zadan 0, definicji obiegow 0 — czyli szef firmy nie mogl zlozyc
/// wniosku urlopowego ani zalozyc zadania. Przyczyna: LeaveSeeder/TaskSeeder/WorkflowSeeder
/// mialy warianty globalne z zaszytym identyfikatorem firmy operatora i warunkiem
/// „przerwij, jesli cokolwiek juz istnieje" — od pierwszego uruchomienia aplikacji nie
/// robily nic dla nikogo poza nia.
///
/// Te testy pilnuja trzech wlasnosci naraz: komplet dla nowej firmy, idempotentnosc
/// (provisioning powtarza sie przy KAZDEJ synchronizacji z Hubem) i brak wplywu na cudze dane.
/// </remarks>
public class SlownikiNowejFirmyTests
{
    private static readonly string[] OczekiwaneTypyUrlopu = ["ANNUAL", "ON_DEMAND", "SICK", "CHILDCARE"];

    [Fact]
    public async Task Nowa_firma_dostaje_komplet_typow_urlopu()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();

        await LeaveSeeder.SeedTenantAsync(db, firma, NullLogger.Instance);

        var kody = await db.Set<LeaveType>().IgnoreQueryFilters()
            .Where(typ => typ.TenantId == firma)
            .Select(typ => typ.Code)
            .ToListAsync();

        Assert.Equal(OczekiwaneTypyUrlopu.Order(), kody.Order());
    }

    [Fact]
    public async Task Nowa_firma_moze_zalozyc_zadanie_bo_ma_statusy_i_priorytety()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();

        await TaskSeeder.SeedTenantAsync(db, firma, NullLogger.Instance);

        var statusy = await db.Set<TaskStatusEntity>().IgnoreQueryFilters()
            .Where(status => status.TenantId == firma).ToListAsync();
        var priorytety = await db.Set<TaskPriority>().IgnoreQueryFilters()
            .Where(priorytet => priorytet.TenantId == firma).CountAsync();

        Assert.Equal(4, statusy.Count);
        Assert.Equal(4, priorytety);
        // Bez statusu domyslnego formularz nowego zadania nie ma czego zaznaczyc.
        Assert.Single(statusy, status => status.IsDefault);
        // Bez statusu koncowego nic nigdy nie zostanie zamkniete.
        Assert.Contains(statusy, status => status.IsFinal);
    }

    [Fact]
    public async Task Nowa_firma_dostaje_obieg_akceptacji()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();

        await WorkflowSeeder.SeedTenantAsync(db, firma);

        var nazwy = await db.Set<WorkflowDefinition>().IgnoreQueryFilters()
            .Where(definicja => definicja.TenantId == firma)
            .Select(definicja => definicja.Name)
            .ToListAsync();

        // Nazwy zamiast liczby: gdy ktos dolozy obieg, test ma powiedziec KTORY doszedl,
        // a nie tylko ze jest ich o jeden wiecej.
        Assert.Equal(
            new[] { "leave-request-v1", "task-acceptance-v1", "wniosek-ogolny-v1" }.Order(),
            nazwy.Order());
    }

    /// <summary>
    /// Provisioning wykonuje sie ponownie przy kazdej synchronizacji z Hubem, takze dla firm
    /// juz istniejacych — dzieki temu starsze firmy dostaja brakujace slowniki bez migracji.
    /// Gdyby seeder nie byl dopisujacy, przy kazdym starcie aplikacji dokladalby duplikaty.
    /// </summary>
    [Fact]
    public async Task Powtorne_zasianie_nie_tworzy_duplikatow()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();

        for (var proba = 0; proba < 3; proba++)
        {
            await LeaveSeeder.SeedTenantAsync(db, firma, NullLogger.Instance);
            await TaskSeeder.SeedTenantAsync(db, firma, NullLogger.Instance);
            await WorkflowSeeder.SeedTenantAsync(db, firma);
        }

        Assert.Equal(4, await Policz<LeaveType>(db, firma));
        Assert.Equal(4, await Policz<TaskStatusEntity>(db, firma));
        Assert.Equal(4, await Policz<TaskPriority>(db, firma));
        Assert.Equal(3, await Policz<WorkflowDefinition>(db, firma));
    }

    /// <summary>
    /// Firma, ktora skasowala u siebie jakis slownik, dostaje go z powrotem przy kolejnej
    /// synchronizacji — ale reszta jej konfiguracji zostaje nietknieta.
    /// </summary>
    [Fact]
    public async Task Brakujacy_pojedynczy_wpis_jest_uzupelniany()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();
        await LeaveSeeder.SeedTenantAsync(db, firma, NullLogger.Instance);

        var doUsuniecia = await db.Set<LeaveType>().IgnoreQueryFilters()
            .FirstAsync(typ => typ.TenantId == firma && typ.Code == "SICK");
        db.Set<LeaveType>().Remove(doUsuniecia);
        await db.SaveChangesAsync();

        await LeaveSeeder.SeedTenantAsync(db, firma, NullLogger.Instance);

        Assert.Equal(4, await Policz<LeaveType>(db, firma));
    }

    [Fact]
    public async Task Zasianie_jednej_firmy_nie_dotyka_drugiej()
    {
        await using var db = UtworzBaze();
        var pierwsza = Guid.NewGuid();
        var druga = Guid.NewGuid();

        await LeaveSeeder.SeedTenantAsync(db, pierwsza, NullLogger.Instance);

        Assert.Equal(4, await Policz<LeaveType>(db, pierwsza));
        Assert.Equal(0, await Policz<LeaveType>(db, druga));
    }

    /// <summary>
    /// Bez ani jednego stanowiska kierowniczego zakres danych „Dzial" nie ma z czego powstac:
    /// EmployeeScopeResolver liczy jednostki, w ktorych uzytkownik zajmuje stanowisko
    /// z IsManagerial. Nowa firma nie dostawala zadnych stanowisk, wiec nikt nigdy nie
    /// zobaczylby danych swojego dzialu — wyszlo przy przejsciu onboardingu od zera.
    /// </summary>
    [Fact]
    public async Task Nowa_firma_dostaje_stanowisko_kierownicze_bo_bez_niego_nie_ma_zakresu_dzialu()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();

        await OrganizationSeeder.SeedTenantStructureAsync(db, firma, "Firma Testowa", NullLogger.Instance);

        var stanowiska = await db.Set<Position>().IgnoreQueryFilters()
            .Where(p => p.TenantId == firma)
            .ToListAsync();

        Assert.Equal(2, stanowiska.Count);
        Assert.Contains(stanowiska, p => p.IsManagerial);
        Assert.Contains(stanowiska, p => !p.IsManagerial);
    }

    /// <summary>
    /// Stanowiska zakladamy PRZED obsluga korzenia struktury, bo tam jest wczesny return dla
    /// firm, ktore korzen juz maja. Gdyby kolejnosc byla odwrotna, firmy zalozone wczesniej
    /// nigdy by stanowisk nie dostaly — a ten seeder wykonuje sie przy kazdej synchronizacji
    /// z Hubem wlasnie po to, zeby uzupelniac braki.
    /// </summary>
    [Fact]
    public async Task Firma_z_istniejacym_korzeniem_tez_dostaje_brakujace_stanowiska()
    {
        await using var db = UtworzBaze();
        var firma = Guid.NewGuid();

        await OrganizationSeeder.SeedTenantStructureAsync(db, firma, "Firma Testowa", NullLogger.Instance);
        db.Set<Position>().RemoveRange(db.Set<Position>().IgnoreQueryFilters().Where(p => p.TenantId == firma));
        await db.SaveChangesAsync();
        Assert.Equal(0, await Policz<Position>(db, firma));

        // Druga synchronizacja — korzen juz istnieje, wiec seeder idzie sciezka wczesnego wyjscia.
        await OrganizationSeeder.SeedTenantStructureAsync(db, firma, "Firma Testowa", NullLogger.Instance);

        Assert.Equal(2, await Policz<Position>(db, firma));
    }

    private static Task<int> Policz<T>(WorkBaseDbContext db, Guid firma) where T : class
        => db.Set<T>().IgnoreQueryFilters()
            .Where(encja => EF.Property<Guid>(encja, "TenantId") == firma)
            .CountAsync();

    private static WorkBaseDbContext UtworzBaze()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"slowniki-nowej-firmy-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}
