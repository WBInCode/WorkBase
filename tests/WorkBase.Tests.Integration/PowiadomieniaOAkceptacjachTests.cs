using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Workflow;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Events;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Powiadomienia o akceptacjach: „cos czeka na Twoja decyzje" i „Twoja sprawa zostala rozpatrzona".
/// </summary>
/// <remarks>
/// Oba zdarzenia byly podnoszone i zadne nie mialo handlera. Widac to bylo wprost na produkcji:
/// w tabeli powiadomien istnialy WYLACZNIE trzy kategorie (anomalie i zadania) — nigdy, ani razu,
/// nikt nie dostal powiadomienia o wniosku do rozpatrzenia ani o decyzji w swojej sprawie.
/// </remarks>
public class PowiadomieniaOAkceptacjachTests
{
    private static readonly Guid Firma = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid KontoAkceptanta = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    private static readonly Guid KontoWnioskodawcy = Guid.Parse("a0000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task Zlozenie_wniosku_powiadamia_akceptanta()
    {
        var (db, powiadomienia, handler, dane) = await Przygotuj();

        await handler.Handle(
            new ApprovalRequestCreatedEvent(
                dane.ZgloszenieId, Firma, dane.PrzebiegId, dane.KrokId, dane.WnioskodawcaId, dane.AkceptantId),
            CancellationToken.None);

        await powiadomienia.Received(1).SendFromTemplateAsync(
            Firma, KontoAkceptanta, "approval_pending",
            Arg.Is<IReadOnlyDictionary<string, string?>>(z =>
                z["rodzaj"] == "Wniosek urlopowy" && z["wnioskodawca"] == "Jan Kowalski"),
            Arg.Any<string>(), Arg.Any<string>(),
            "approval_pending", "approval_request", dane.ZgloszenieId, Arg.Any<CancellationToken>());
        await db.DisposeAsync();
    }

    /// <summary>
    /// Firma jednoosobowa i wlasciciel zatwierdzajacy wlasny wniosek. „Masz do rozpatrzenia to,
    /// co wlasnie zlozyles" to czysty halas.
    /// </summary>
    [Fact]
    public async Task Samoakceptacja_nie_generuje_powiadomienia()
    {
        var (db, powiadomienia, handler, dane) = await Przygotuj();

        await handler.Handle(
            new ApprovalRequestCreatedEvent(
                dane.ZgloszenieId, Firma, dane.PrzebiegId, dane.KrokId, dane.AkceptantId, dane.AkceptantId),
            CancellationToken.None);

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Decyzja_powiadamia_wnioskodawce()
    {
        var (db, powiadomienia, handler, dane) = await Przygotuj();

        await handler.Handle(
            new ApprovalDecisionMadeEvent(
                Guid.NewGuid(), dane.ZgloszenieId, Firma, dane.PrzebiegId, dane.AkceptantId, "approve"),
            CancellationToken.None);

        await powiadomienia.Received(1).SendFromTemplateAsync(
            Firma, KontoWnioskodawcy, "approval_decided",
            Arg.Is<IReadOnlyDictionary<string, string?>>(z => z["decyzja"] == "Zaakceptowano"),
            Arg.Any<string>(), Arg.Any<string>(),
            "approval_decided", "approval_request", dane.ZgloszenieId, Arg.Any<CancellationToken>());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Odrzucenie_tez_dochodzi_do_wnioskodawcy()
    {
        var (db, powiadomienia, handler, dane) = await Przygotuj();

        await handler.Handle(
            new ApprovalDecisionMadeEvent(
                Guid.NewGuid(), dane.ZgloszenieId, Firma, dane.PrzebiegId, dane.AkceptantId, "reject"),
            CancellationToken.None);

        await powiadomienia.Received(1).SendFromTemplateAsync(
            Firma, KontoWnioskodawcy, "approval_decided",
            Arg.Is<IReadOnlyDictionary<string, string?>>(z => z["decyzja"] == "Odrzucono"),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
        await db.DisposeAsync();
    }

    /// <summary>Kto decydowal, ten wie.</summary>
    [Fact]
    public async Task Decyzja_o_wlasnym_wniosku_nie_wraca_do_decydenta()
    {
        var (db, powiadomienia, handler, dane) = await Przygotuj();

        await handler.Handle(
            new ApprovalDecisionMadeEvent(
                Guid.NewGuid(), dane.ZgloszenieId, Firma, dane.PrzebiegId, dane.WnioskodawcaId, "approve"),
            CancellationToken.None);

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    /// <summary>Akceptant bez konta (praca przy terminalu) — nie ma komu wyslac.</summary>
    [Fact]
    public async Task Akceptant_bez_konta_nie_generuje_powiadomienia()
    {
        var (db, powiadomienia, handler, dane) = await Przygotuj(akceptantZKontem: false);

        await handler.Handle(
            new ApprovalRequestCreatedEvent(
                dane.ZgloszenieId, Firma, dane.PrzebiegId, dane.KrokId, dane.WnioskodawcaId, dane.AkceptantId),
            CancellationToken.None);

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    private sealed record Dane(
        Guid ZgloszenieId, Guid PrzebiegId, Guid KrokId, Guid WnioskodawcaId, Guid AkceptantId);

    private static async Task<(WorkBaseDbContext, INotificationService, PowiadomieniaOAkceptacjach, Dane)>
        Przygotuj(bool akceptantZKontem = true)
    {
        var db = new WorkBaseDbContext(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"akceptacje-{Guid.NewGuid():N}")
            .Options);

        var wnioskodawca = Employee.Create(
            Firma, "Jan", "Kowalski", "jan@example.com", null, DateTime.UtcNow.AddYears(-1));
        wnioskodawca.LinkUser(KontoWnioskodawcy);

        var akceptant = Employee.Create(
            Firma, "Anna", "Nowak", "anna@example.com", null, DateTime.UtcNow.AddYears(-2));
        if (akceptantZKontem) akceptant.LinkUser(KontoAkceptanta);

        db.AddRange(wnioskodawca, akceptant);

        var przebieg = WorkflowInstance.Create(
            Firma, Guid.NewGuid(), "LeaveRequest", Guid.NewGuid(), "Akceptacja przełożonego", wnioskodawca.Id);
        db.Add(przebieg);
        await db.SaveChangesAsync();

        var krok = WorkflowStep.Create(Firma, przebieg.Id, "Akceptacja przełożonego");
        db.Add(krok);
        await db.SaveChangesAsync();

        var zgloszenie = ApprovalRequest.Create(
            Firma, krok.Id, przebieg.Id, wnioskodawca.Id, akceptant.Id);
        db.Add(zgloszenie);
        await db.SaveChangesAsync();

        var powiadomienia = Substitute.For<INotificationService>();
        var handler = new PowiadomieniaOAkceptacjach(
            db, powiadomienia, NullLogger<PowiadomieniaOAkceptacjach>.Instance);

        return (db, powiadomienia, handler,
            new Dane(zgloszenie.Id, przebieg.Id, krok.Id, wnioskodawca.Id, akceptant.Id));
    }
}
