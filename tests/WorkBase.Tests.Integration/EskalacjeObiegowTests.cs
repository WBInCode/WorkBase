using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Workflow;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Entities;
using Xunit;
using Powiadomienie = WorkBase.Modules.Notification.Domain.Entities.Notification;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Eskalacje obiegow — wniosek, ktory stoi u akceptanta dluzej niz firma ustalila.
/// </summary>
/// <remarks>
/// EscalationRule miala encje, repozytorium, komendy, endpointy I GOTOWY EKRAN, ale zaden job
/// nigdy tych regul nie ewaluowal. Administrator ustawial prog czasu i nic sie nie dzialo.
///
/// Najwazniejsza wlasnosc: powiadomienie idzie RAZ na zgloszenie, nie raz na przebieg. Job chodzi
/// co 15 minut, a wniosek potrafi stac tygodniami — bez tego jedno zgloszenie daloby setki
/// powiadomien. Ta sama pulapka wyszla juz przy zaleglych zadaniach i terminach kadrowych.
/// </remarks>
public class EskalacjeObiegowTests
{
    private static readonly Guid Firma = Guid.Parse("70000000-0000-0000-0000-000000000001");
    private static readonly Guid Konto = Guid.Parse("70000000-0000-0000-0000-000000000002");
    private static readonly Guid Definicja = Guid.Parse("70000000-0000-0000-0000-000000000003");
    private const string Krok = "Akceptacja przelozonego";

    [Fact]
    public async Task Zgloszenie_ponad_progiem_powoduje_powiadomienie_akceptanta()
    {
        var (db, powiadomienia, job, _) = await Przygotuj(czekaOdGodzin: 5, progMinut: 60);

        await job.ExecuteAsync();

        await powiadomienia.Received(1).SendFromTemplateAsync(
            Firma, Konto, "escalation", Arg.Any<IReadOnlyDictionary<string, string?>>(),
            Arg.Any<string>(), Arg.Any<string>(),
            "escalation", "approval_request", Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Zgloszenie_ponizej_progu_jest_pomijane()
    {
        var (db, powiadomienia, job, _) = await Przygotuj(czekaOdGodzin: 1, progMinut: 240);

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    /// <summary>
    /// Job chodzi co 15 minut. Bez tego zabezpieczenia wniosek stojacy tydzien daloby
    /// szescset kilkadziesiat powiadomien.
    /// </summary>
    [Fact]
    public async Task Kolejny_przebieg_nie_powtarza_powiadomienia()
    {
        var (db, powiadomienia, job, zgloszenieId) = await Przygotuj(czekaOdGodzin: 5, progMinut: 60);

        db.Add(Powiadomienie.Create(
            Firma, Konto, "cokolwiek", "cokolwiek", "escalation", "approval_request", zgloszenieId));
        await db.SaveChangesAsync();

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    [Fact]
    public async Task Regula_wylaczona_nie_dziala()
    {
        var (db, powiadomienia, job, _) = await Przygotuj(czekaOdGodzin: 5, progMinut: 60, regulaAktywna: false);

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    /// <summary>
    /// Regula wiaze konkretny KROK. Zgloszenie z innego kroku tej samej definicji nie moze
    /// jej podlegac — inaczej jedna regula eskalowalaby caly obieg.
    /// </summary>
    [Fact]
    public async Task Zgloszenie_z_innego_kroku_nie_podlega_regule()
    {
        var (db, powiadomienia, job, _) = await Przygotuj(
            czekaOdGodzin: 5, progMinut: 60, nazwaKroku: "Zupelnie inny krok");

        await job.ExecuteAsync();

        await powiadomienia.DidNotReceiveWithAnyArgs().SendFromTemplateAsync(
            default, default, default!, default!, default!, default!, default!, default, default, default);
        await db.DisposeAsync();
    }

    private static async Task<(WorkBaseDbContext, INotificationService, EskalacjeObiegowJob, Guid)> Przygotuj(
        int czekaOdGodzin,
        int progMinut,
        bool regulaAktywna = true,
        string? nazwaKroku = null)
    {
        var db = new WorkBaseDbContext(new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"eskalacje-{Guid.NewGuid():N}")
            .Options);

        var akceptant = Employee.Create(Firma, "Anna", "Nowak", "anna@example.com", null, DateTime.UtcNow.AddYears(-2));
        akceptant.LinkUser(Konto);
        db.Add(akceptant);

        var instancja = WorkflowInstance.Create(Firma, Definicja, "Wniosek", Guid.NewGuid(), Krok, Guid.NewGuid());
        db.Add(instancja);
        await db.SaveChangesAsync();

        var krok = WorkflowStep.Create(Firma, instancja.Id, nazwaKroku ?? Krok);
        db.Add(krok);
        await db.SaveChangesAsync();

        var zgloszenie = ApprovalRequest.Create(
            Firma, krok.Id, instancja.Id, Guid.NewGuid(), akceptant.Id);
        db.Add(zgloszenie);
        await db.SaveChangesAsync();

        // CreatedAt ustawia audyt przy zapisie, wiec cofamy go wprost — inaczej kazde
        // zgloszenie bylo by "swieze" i zaden prog nigdy by nie zadzialal.
        db.Entry(zgloszenie).Property(z => z.CreatedAt).CurrentValue = DateTime.UtcNow.AddHours(-czekaOdGodzin);

        var regula = EscalationRule.Create(Firma, Definicja, Krok, progMinut, "notify");
        if (!regulaAktywna) regula.Deactivate();
        db.Add(regula);
        await db.SaveChangesAsync();

        var powiadomienia = Substitute.For<INotificationService>();
        var job = new EskalacjeObiegowJob(db, powiadomienia, NullLogger<EskalacjeObiegowJob>.Instance);

        return (db, powiadomienia, job, zgloszenie.Id);
    }
}
