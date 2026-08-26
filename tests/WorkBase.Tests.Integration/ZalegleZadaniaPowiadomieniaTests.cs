using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Tasks;
using WorkBase.Modules.Notification.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Events;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Powiadomienie o zadaniu po terminie.
/// </summary>
/// <remarks>
/// <c>TaskOverdueDetectorJob</c> chodzil codziennie o 06:00 i publikowal <c>TaskOverdueEvent</c>,
/// ktorego nikt nie obslugiwal. Podpiecie handlera bez zabezpieczenia zamienialo by martwy kod
/// w spam: job publikuje zdarzenie przy KAZDYM przebiegu dla kazdego zaleglego zadania, wiec
/// zadanie zalegle miesiac dawaloby trzydziesci powiadomien. Te testy pilnuja, ze wysylka
/// nastepuje raz na zadanie.
/// </remarks>
public class ZalegleZadaniaPowiadomieniaTests
{
    private static readonly Guid Firma = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Zadanie = Guid.Parse("40000000-0000-0000-0000-000000000002");
    private static readonly Guid Pracownik = Guid.Parse("40000000-0000-0000-0000-000000000003");
    private static readonly Guid Konto = Guid.Parse("40000000-0000-0000-0000-000000000004");

    [Fact]
    public async Task Pierwsze_przekroczenie_terminu_wysyla_powiadomienie()
    {
        await using var db = UtworzBaze();
        var powiadomienia = Substitute.For<INotificationService>();
        var handler = Zbuduj(db, powiadomienia, kontoPracownika: Konto);

        await handler.Handle(Zdarzenie(), CancellationToken.None);

        await powiadomienia.Received(1).SendAsync(
            Firma, Konto, "Zadanie po terminie", Arg.Any<string>(),
            "task_overdue", "task", Zadanie, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Kolejny_przebieg_tego_samego_dnia_juz_nie_wysyla()
    {
        await using var db = UtworzBaze();
        var powiadomienia = Substitute.For<INotificationService>();
        var handler = Zbuduj(db, powiadomienia, kontoPracownika: Konto);

        // Slad po wczorajszym przebiegu joba.
        db.Set<Notification>().Add(Notification.Create(
            Firma, Konto, "Zadanie po terminie", "cokolwiek", "task_overdue", "task", Zadanie));
        await db.SaveChangesAsync();

        await handler.Handle(Zdarzenie(), CancellationToken.None);

        await powiadomienia.DidNotReceiveWithAnyArgs().SendAsync(
            default, default, default!, default!, default!, default, default, default);
    }

    [Fact]
    public async Task Powiadomienie_o_INNYM_zadaniu_nie_blokuje_wysylki()
    {
        await using var db = UtworzBaze();
        var powiadomienia = Substitute.For<INotificationService>();
        var handler = Zbuduj(db, powiadomienia, kontoPracownika: Konto);

        db.Set<Notification>().Add(Notification.Create(
            Firma, Konto, "Zadanie po terminie", "inne zadanie", "task_overdue", "task", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await handler.Handle(Zdarzenie(), CancellationToken.None);

        await powiadomienia.Received(1).SendAsync(
            Firma, Konto, Arg.Any<string>(), Arg.Any<string>(),
            "task_overdue", "task", Zadanie, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pracownik_bez_konta_nie_generuje_powiadomienia()
    {
        await using var db = UtworzBaze();
        var powiadomienia = Substitute.For<INotificationService>();
        var handler = Zbuduj(db, powiadomienia, kontoPracownika: null);

        await handler.Handle(Zdarzenie(), CancellationToken.None);

        await powiadomienia.DidNotReceiveWithAnyArgs().SendAsync(
            default, default, default!, default!, default!, default, default, default);
    }

    private static TaskOverdueEvent Zdarzenie() =>
        new(Zadanie, Firma, Pracownik, "Rozliczyć fakturę", DateTime.UtcNow.AddDays(-3));

    private static PowiadomOZaleglymZadaniu Zbuduj(
        WorkBaseDbContext db, INotificationService powiadomienia, Guid? kontoPracownika)
    {
        var organizacja = Substitute.For<IOrganizationLookupService>();
        organizacja.GetUserIdByEmployeeIdAsync(Pracownik, Arg.Any<CancellationToken>())
            .Returns(kontoPracownika);

        return new PowiadomOZaleglymZadaniu(
            db, organizacja, powiadomienia, NullLogger<PowiadomOZaleglymZadaniu>.Instance);
    }

    private static WorkBaseDbContext UtworzBaze()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"zalegle-zadania-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }
}
