using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.ListyKontrolne;

public sealed record PozycjaListyDto(string Tytul, int DniOdZdarzenia, string Wykonawca, Guid? OsobaId);

public sealed record ListaKontrolnaDto(
    Guid Id, string Nazwa, string Wyzwalacz, bool Aktywna, IReadOnlyList<PozycjaListyDto> Pozycje);

internal static class ListyMapowanie
{
    public static ListaKontrolnaDto DoDto(this ListaKontrolna l) => new(
        l.Id, l.Nazwa, l.Wyzwalacz.ToString(), l.Aktywna,
        l.Pozycje.OrderBy(p => p.Kolejnosc)
            .Select(p => new PozycjaListyDto(p.Tytul, p.DniOdZdarzenia, p.Wykonawca.ToString(), p.OsobaId))
            .ToList());
}

/// <summary>Zapis całej listy naraz — nazwa, wyzwalacz, flaga i komplet pozycji.</summary>
public sealed record ZapiszListeKontrolnaCommand(
    Guid? Id, string Nazwa, string Wyzwalacz, bool Aktywna, IReadOnlyList<PozycjaListyDto> Pozycje)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZapiszListeKontrolnaHandler(IListaKontrolnaRepository listy)
    : ICommandHandler<ZapiszListeKontrolnaCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ZapiszListeKontrolnaCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<WyzwalaczListy>(request.Wyzwalacz, ignoreCase: true, out var wyzwalacz))
            return Result.Failure<Guid>(Error.Validation("Lista.Wyzwalacz", "Nieznany wyzwalacz listy."));

        var pozycje = new List<(string, int, WykonawcaPozycji, Guid?)>();
        foreach (var p in request.Pozycje)
        {
            if (!Enum.TryParse<WykonawcaPozycji>(p.Wykonawca, ignoreCase: true, out var wykonawca))
                return Result.Failure<Guid>(Error.Validation("Lista.Wykonawca", $"Nieznany wykonawca „{p.Wykonawca}”."));
            pozycje.Add((p.Tytul, p.DniOdZdarzenia, wykonawca, p.OsobaId));
        }

        try
        {
            ListaKontrolna lista;
            if (request.Id is Guid id)
            {
                var istniejaca = await listy.PobierzAsync(id, ct);
                if (istniejaca is null)
                    return Result.Failure<Guid>(Error.NotFound("Lista.NieIstnieje", "Lista nie istnieje."));
                istniejaca.Zmien(request.Nazwa, wyzwalacz, request.Aktywna);
                lista = istniejaca;
            }
            else
            {
                lista = ListaKontrolna.Utworz(request.TenantId, request.Nazwa, wyzwalacz, request.Aktywna);
                await listy.DodajAsync(lista, ct);
            }

            lista.UstawPozycje(pozycje);
            return lista.Id;
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(Error.Validation("Lista.Niepoprawna", ex.Message));
        }
    }
}

public sealed record PobierzListyKontrolneQuery : IQuery<List<ListaKontrolnaDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzListyKontrolneHandler(IListaKontrolnaRepository listy)
    : IQueryHandler<PobierzListyKontrolneQuery, List<ListaKontrolnaDto>>
{
    public async Task<Result<List<ListaKontrolnaDto>>> Handle(PobierzListyKontrolneQuery request, CancellationToken ct)
    {
        var wszystkie = await listy.PobierzWszystkieAsync(ct);
        return wszystkie
            .OrderBy(l => l.Wyzwalacz).ThenBy(l => l.Nazwa)
            .Select(l => l.DoDto())
            .ToList();
    }
}
