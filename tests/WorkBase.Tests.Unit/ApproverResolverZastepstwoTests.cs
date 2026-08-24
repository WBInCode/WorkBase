using WorkBase.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using Xunit;

namespace WorkBase.Tests.Unit;

/// <summary>
/// Wyznaczanie akceptanta z uwzglednieniem zastepstwa.
/// </summary>
/// <remarks>
/// ApproverResolver to JEDYNE miejsce w systemie, w ktorym rozstrzyga sie „kto to zatwierdza" —
/// dlatego zastepstwo wpiete jest tam, a nie przy wnioskach urlopowych. Te testy pilnuja, ze
/// podmiana dziala, oraz ze lancuch zastepstw nie potrafi zawiesic obiegu.
/// </remarks>
public class ApproverResolverZastepstwoTests
{
    private static readonly Guid Pracownik = Guid.NewGuid();
    private static readonly Guid Kierownik = Guid.NewGuid();
    private static readonly Guid Zastepca = Guid.NewGuid();
    private static readonly Guid DrugiZastepca = Guid.NewGuid();

    [Fact]
    public async Task Bez_zastepstwa_akceptantem_jest_przelozony()
    {
        var resolver = new ApproverResolver(new FakeLookup(Kierownik));

        var wynik = await resolver.ResolveApproverAsync("supervisor", Pracownik);

        Assert.True(wynik.IsSuccess);
        Assert.Equal(Kierownik, wynik.Value);
    }

    [Fact]
    public async Task Gdy_przelozony_ma_zastepce_wniosek_idzie_do_zastepcy()
    {
        var lookup = new FakeLookup(Kierownik);
        lookup.Zastepstwa[Kierownik] = Zastepca;
        var resolver = new ApproverResolver(lookup);

        var wynik = await resolver.ResolveApproverAsync("supervisor", Pracownik);

        Assert.Equal(Zastepca, wynik.Value);
    }

    /// <summary>
    /// Kierownik i jego zastepca na tym samym szkoleniu — bez podazania lancucha wniosek
    /// wyladowalby u osoby, ktorej rowniez nie ma.
    /// </summary>
    [Fact]
    public async Task Gdy_zastepca_tez_ma_zastepce_lancuch_jest_podazany()
    {
        var lookup = new FakeLookup(Kierownik);
        lookup.Zastepstwa[Kierownik] = Zastepca;
        lookup.Zastepstwa[Zastepca] = DrugiZastepca;
        var resolver = new ApproverResolver(lookup);

        var wynik = await resolver.ResolveApproverAsync("supervisor", Pracownik);

        Assert.Equal(DrugiZastepca, wynik.Value);
    }

    /// <summary>
    /// Dwie osoby wskazuja siebie nawzajem. Bez wykrywania petli rozwiazywanie akceptanta
    /// kreciloby sie w kolko i obieg by stanal.
    /// </summary>
    [Fact]
    public async Task Wzajemne_zastepstwo_nie_zawiesza_wyznaczania_akceptanta()
    {
        var lookup = new FakeLookup(Kierownik);
        lookup.Zastepstwa[Kierownik] = Zastepca;
        lookup.Zastepstwa[Zastepca] = Kierownik;
        var resolver = new ApproverResolver(lookup);

        var wynik = await resolver.ResolveApproverAsync("supervisor", Pracownik);

        Assert.True(wynik.IsSuccess);
        Assert.Equal(Zastepca, wynik.Value);
    }

    [Fact]
    public async Task Brak_przelozonego_nadal_konczy_sie_czytelnym_bledem()
    {
        var resolver = new ApproverResolver(new FakeLookup(przelozony: null));

        var wynik = await resolver.ResolveApproverAsync("supervisor", Pracownik);

        Assert.True(wynik.IsFailure);
        Assert.Equal("Approval.SupervisorNotFound", wynik.Error.Code);
    }

    private sealed class FakeLookup(Guid? przelozony) : ISupervisorLookupService
    {
        public Dictionary<Guid, Guid> Zastepstwa { get; } = [];

        public Task<Guid?> GetSupervisorEmployeeIdAsync(Guid subordinateEmployeeId, CancellationToken ct = default)
            => Task.FromResult(przelozony);

        // Inicjator jest tu identyfikatorem pracownika, nie konta — tak jak przy wniosku urlopowym.
        public Task<Guid?> GetEmployeeIdByUserIdAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<Guid?>(null);

        public Task<bool> HasSubordinatesAsync(Guid supervisorEmployeeId, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<Guid?> GetZastepceAsync(Guid zastepowanyEmployeeId, DateOnly dzien, CancellationToken ct = default)
            => Task.FromResult(Zastepstwa.TryGetValue(zastepowanyEmployeeId, out var z) ? z : (Guid?)null);
    }
}
