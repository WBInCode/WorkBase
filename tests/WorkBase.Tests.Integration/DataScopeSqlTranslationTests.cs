using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using WorkBase.Infrastructure.Auth;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Zakres danych czytamy przez prawdziwego Postgresa, bo blad, ktory te testy pilnuja,
/// istnieje wylacznie w tlumaczeniu LINQ na SQL. Dostawca in-memory przepuszczal
/// rzutowanie (int)ScopeLevel w projekcji, a Npgsql generowal z niego scope_level::int
/// i produkcja odrzucala zapytanie (22P02) — caly modul urlopow zwracal 500.
/// </summary>
public sealed class DataScopeSqlTranslationTests : IAsyncLifetime
{
    private static readonly Guid TenantId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid EmployeeId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    private static string? ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    private WorkBaseDbContext _db = null!;
    private bool _skip;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            _skip = true;
            return;
        }

        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseNpgsql(ConnectionString)
            // Ta sama konwencja nazw co w aplikacji — bez niej zapytania szukalyby "Id" zamiast "id".
            .UseSnakeCaseNamingConvention()
            // Model rozjechal sie z migracjami (osobna sprawa, sprzed tych testow) —
            // bez tego MigrateAsync odmawia startu i test nie mialby czego sprawdzic.
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        _db = new WorkBaseDbContext(options);
        // Migracje, a nie EnsureCreated — schemat ma byc taki sam jak na produkcji.
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_skip) await _db.DisposeAsync();
    }

    [Fact]
    public async Task Odczyt_zakresu_danych_nie_wywala_sie_na_tlumaczeniu_do_SQL()
    {
        if (_skip) return; // bez Postgresa test nie ma czego sprawdzic

        var user = await ArrangeUserWithScope(DataScopeLevel.Organization);
        var resolver = new EmployeeScopeResolver(_db, new MemoryCache(new MemoryCacheOptions()));

        // Przed poprawka to zapytanie konczylo sie PostgresException 22P02.
        var dostep = await resolver.CanAccessEmployeeAsync(
            user.Id, TenantId, EmployeeId, EmployeeId, "leave");

        Assert.True(dostep);
    }

    [Fact]
    public async Task Efektywny_zakres_danych_czyta_sie_z_bazy_bez_bledu()
    {
        if (_skip) return;

        var user = await ArrangeUserWithScope(DataScopeLevel.Department);
        var service = new DataScopeService(_db, new MemoryCache(new MemoryCacheOptions()));

        var wynik = await service.GetEffectiveScopeAsync(user.Id, TenantId, "leave");

        Assert.Equal(DataScopeLevelValue.Department, wynik.Level);
    }

    /// <summary>
    /// Wypisanie widocznych pracownikow bez listy kandydatow — sciezka pulpitu.
    /// </summary>
    /// <remarks>
    /// To jest dokladnie ta klasa bledu, dla ktorej powstal ten plik. Zapytania maja warunek
    /// `ograniczDo == null || ograniczDo.Contains(...)`, gdzie `ograniczDo` jest kolekcja albo
    /// nullem. Dostawca in-memory wykona taki warunek w pamieci i przepusci wszystko; Npgsql
    /// musi go PRZETLUMACZYC. Bez tego testu pierwsze wejscie na pulpit na produkcji byloby
    /// pierwszym uruchomieniem tego kodu przeciw prawdziwej bazie.
    /// </remarks>
    [Theory]
    [InlineData(DataScopeLevel.Own)]
    [InlineData(DataScopeLevel.Team)]
    [InlineData(DataScopeLevel.Department)]
    public async Task Wypisanie_widocznych_pracownikow_tlumaczy_sie_do_SQL(DataScopeLevel poziom)
    {
        if (_skip) return;

        var user = await ArrangeUserWithScope(poziom);
        var resolver = new EmployeeScopeResolver(_db, new MemoryCache(new MemoryCacheOptions()));

        var widoczni = await resolver.GetVisibleEmployeeIdsAsync(user.Id, TenantId, EmployeeId, "leave");

        // Zawezenie musi istniec i obejmowac przynajmniej pytajacego — null oznaczaloby
        // „bez ograniczenia", czyli liczby calej firmy dla kogos, kto nie ma takiego zakresu.
        Assert.NotNull(widoczni);
        Assert.Contains(EmployeeId, widoczni!);
    }

    [Fact]
    public async Task Zakres_calej_firmy_nie_odpytuje_bazy_i_zwraca_brak_ograniczenia()
    {
        if (_skip) return;

        var user = await ArrangeUserWithScope(DataScopeLevel.Organization);
        var resolver = new EmployeeScopeResolver(_db, new MemoryCache(new MemoryCacheOptions()));

        Assert.Null(await resolver.GetVisibleEmployeeIdsAsync(user.Id, TenantId, EmployeeId, "leave"));
    }

    [Fact]
    public void Oba_typy_poziomu_zakresu_maja_zgodne_wartosci_liczbowe()
    {
        // Kod mapuje jeden typ na drugi rzutowaniem przez int, wiec rozjazd
        // wartosci po cichu nadalby lub odebral dostep do cudzych danych.
        Assert.Equal((int)DataScopeLevelValue.Own, (int)DataScopeLevel.Own);
        Assert.Equal((int)DataScopeLevelValue.Team, (int)DataScopeLevel.Team);
        Assert.Equal((int)DataScopeLevelValue.Department, (int)DataScopeLevel.Department);
        Assert.Equal((int)DataScopeLevelValue.Branch, (int)DataScopeLevel.Branch);
        Assert.Equal((int)DataScopeLevelValue.Organization, (int)DataScopeLevel.Organization);
    }

    private async Task<User> ArrangeUserWithScope(DataScopeLevel level)
    {
        var znacznik = Guid.NewGuid().ToString("N")[..8];
        var user = User.Create($"scope-{znacznik}", $"{znacznik}@example.com", "Test", "Zakres", TenantId);
        var role = Role.Create(TenantId, $"Rola {znacznik}", RoleType.Organizational, level: 10);
        _db.AddRange(user, role);
        await _db.SaveChangesAsync();
        _db.Add(UserRole.Create(user.Id, role.Id, TenantId, "test"));
        _db.Add(DataScope.Create(TenantId, role.Id, "leave", level));
        await _db.SaveChangesAsync();
        return user;
    }
}
