using WorkBase.Contracts;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

// ─── typy wniosków ───

public sealed record TypWnioskuDto(
    Guid Id,
    string Kod,
    string Nazwa,
    string? Opis,
    IReadOnlyList<PoleWniosku> Pola,
    bool WymagaAkceptacji,
    bool Aktywny);

public sealed record UtworzTypWnioskuCommand(
    string Kod,
    string Nazwa,
    string? Opis,
    IReadOnlyList<PoleWniosku> Pola,
    bool WymagaAkceptacji) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UtworzTypWnioskuHandler(ITypWnioskuRepository typy)
    : ICommandHandler<UtworzTypWnioskuCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UtworzTypWnioskuCommand request, CancellationToken ct)
    {
        if (await typy.IstniejeKodAsync(request.TenantId, request.Kod, ct))
        {
            return Result.Failure<Guid>(new Error(
                "TypWniosku.KodZajety", $"Typ wniosku o kodzie „{request.Kod}” już istnieje."));
        }

        var wynik = TypWniosku.Utworz(
            request.TenantId, request.Kod, request.Nazwa, request.Pola,
            request.WymagaAkceptacji, request.Opis);

        if (wynik.IsFailure) return Result.Failure<Guid>(wynik.Error);

        await typy.DodajAsync(wynik.Value, ct);
        return wynik.Value.Id;
    }
}

public sealed record ZmienTypWnioskuCommand(
    Guid Id,
    string Nazwa,
    string? Opis,
    IReadOnlyList<PoleWniosku> Pola,
    bool WymagaAkceptacji,
    bool Aktywny) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZmienTypWnioskuHandler(ITypWnioskuRepository typy)
    : ICommandHandler<ZmienTypWnioskuCommand>
{
    public async Task<Result> Handle(ZmienTypWnioskuCommand request, CancellationToken ct)
    {
        var typ = await typy.PobierzAsync(request.TenantId, request.Id, ct);
        if (typ is null)
            return Result.Failure(Error.NotFound("TypWniosku.NieZnaleziono", "Nie znaleziono typu wniosku."));

        var wynik = typ.Zmien(request.Nazwa, request.Pola, request.WymagaAkceptacji, request.Opis);
        if (wynik.IsFailure) return wynik;

        if (request.Aktywny) typ.Wlacz();
        else typ.Wylacz();

        return Result.Success();
    }
}

public sealed record PobierzTypyWnioskowQuery(bool TylkoAktywne)
    : IQuery<List<TypWnioskuDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzTypyWnioskowHandler(ITypWnioskuRepository typy)
    : IQueryHandler<PobierzTypyWnioskowQuery, List<TypWnioskuDto>>
{
    public async Task<Result<List<TypWnioskuDto>>> Handle(PobierzTypyWnioskowQuery request, CancellationToken ct)
    {
        var lista = await typy.PobierzWszystkieAsync(request.TenantId, request.TylkoAktywne, ct);

        return lista
            .Select(t => new TypWnioskuDto(
                t.Id, t.Kod, t.Nazwa, t.Opis, t.Pola(), t.WymagaAkceptacji, t.Aktywny))
            .ToList();
    }
}

// ─── wnioski ───

public sealed record WniosekDto(
    Guid Id,
    Guid TypWnioskuId,
    string TypNazwa,
    string Status,
    IReadOnlyDictionary<string, string?> Wartosci,
    DateTime ZlozonyO,
    DateTime? RozstrzygnietyO);

public sealed record ZlozWniosekCommand(
    Guid TypWnioskuId,
    Guid EmployeeId,
    IReadOnlyDictionary<string, string?> Wartosci) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Składa wniosek i — jeśli typ tego wymaga — uruchamia obieg akceptacji.
/// </summary>
/// <remarks>
/// Ta sama definicja obiegu (<c>wniosek-ogolny-v1</c>) obsługuje wszystkie typy wniosków, bo
/// silnik nie musi wiedzieć, czego wniosek dotyczy. Akceptanta wyznacza strategia
/// „supervisor”, więc zastępstwa działają tu bez żadnej dodatkowej pracy.
/// </remarks>
public sealed class ZlozWniosekHandler(
    ITypWnioskuRepository typy,
    IWnioskiRepository wnioski,
    IWorkflowService obiegi) : ICommandHandler<ZlozWniosekCommand, Guid>
{
    private const string DefinicjaObiegu = "wniosek-ogolny-v1";

    public async Task<Result<Guid>> Handle(ZlozWniosekCommand request, CancellationToken ct)
    {
        var typ = await typy.PobierzAsync(request.TenantId, request.TypWnioskuId, ct);
        if (typ is null)
            return Result.Failure<Guid>(Error.NotFound("TypWniosku.NieZnaleziono", "Nie znaleziono typu wniosku."));

        if (!typ.Aktywny)
        {
            return Result.Failure<Guid>(new Error(
                "TypWniosku.Wylaczony", "Ten rodzaj wniosku został wyłączony i nie można go już składać."));
        }

        var bledy = typ.SprawdzWartosci(request.Wartosci);
        if (bledy.Count > 0)
        {
            return Result.Failure<Guid>(new Error(
                "Wniosek.NiepoprawnyFormularz", string.Join(" ", bledy)));
        }

        var wniosek = Wniosek.Zloz(
            request.TenantId, typ.Id, request.EmployeeId, request.Wartosci, typ.WymagaAkceptacji);

        if (typ.WymagaAkceptacji)
        {
            var instancja = await obiegi.CreateInstanceAsync(
                request.TenantId,
                DefinicjaObiegu,
                Wniosek.TypEncjiWObiegu,
                wniosek.Id,
                request.EmployeeId,
                initialOutcome: "submitted",
                approvalDueDate: null,
                cancellationToken: ct);

            if (instancja.HasValue) wniosek.PowiazZObiegiem(instancja.Value);
        }

        await wnioski.DodajAsync(wniosek, ct);
        return wniosek.Id;
    }
}

public sealed record AnulujWniosekCommand(Guid Id, Guid EmployeeId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AnulujWniosekHandler(IWnioskiRepository wnioski)
    : ICommandHandler<AnulujWniosekCommand>
{
    public async Task<Result> Handle(AnulujWniosekCommand request, CancellationToken ct)
    {
        var wniosek = await wnioski.PobierzAsync(request.TenantId, request.Id, ct);
        if (wniosek is null)
            return Result.Failure(Error.NotFound("Wniosek.NieZnaleziono", "Nie znaleziono wniosku."));

        // Wycofac wniosek moze wylacznie ten, kto go zlozyl. Przelozony go odrzuca, a to
        // inna decyzja i inny slad w historii.
        if (wniosek.EmployeeId != request.EmployeeId)
        {
            return Result.Failure(new Error(
                "Wniosek.NieTwoj", "Wycofać wniosek może tylko osoba, która go złożyła."));
        }

        return wniosek.Anuluj();
    }
}

public sealed record PobierzMojeWnioskiQuery(Guid EmployeeId)
    : IQuery<List<WniosekDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzMojeWnioskiHandler(
    IWnioskiRepository wnioski,
    ITypWnioskuRepository typy) : IQueryHandler<PobierzMojeWnioskiQuery, List<WniosekDto>>
{
    public async Task<Result<List<WniosekDto>>> Handle(PobierzMojeWnioskiQuery request, CancellationToken ct)
    {
        var lista = await wnioski.PobierzPracownikaAsync(request.TenantId, request.EmployeeId, ct);
        if (lista.Count == 0) return new List<WniosekDto>();

        // Jedno pobranie typow zamiast zapytania na wniosek — lista bywa dluga, a typow
        // jest kilka.
        var wszystkieTypy = (await typy.PobierzWszystkieAsync(request.TenantId, tylkoAktywne: false, ct))
            .ToDictionary(t => t.Id, t => t.Nazwa);

        return lista
            .Select(w => new WniosekDto(
                w.Id,
                w.TypWnioskuId,
                wszystkieTypy.TryGetValue(w.TypWnioskuId, out var nazwa) ? nazwa : "(usunięty typ)",
                w.Status.ToString(),
                w.Wartosci(),
                w.ZlozonyO,
                w.RozstrzygnietyO))
            .ToList();
    }
}
