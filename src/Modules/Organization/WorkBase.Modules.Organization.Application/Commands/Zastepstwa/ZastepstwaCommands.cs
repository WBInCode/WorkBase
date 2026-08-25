using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Zastepstwa;

public sealed record ZastepstwoDto(
    Guid Id,
    Guid ZastepowanyEmployeeId,
    Guid ZastepcaEmployeeId,
    string ZastepcaImieNazwisko,
    DateOnly OdKiedy,
    DateOnly DoKiedy,
    string? Powod,
    bool ObowiazujeDzis);

public sealed record WyznaczZastepstwoCommand(
    Guid ZastepowanyEmployeeId,
    Guid ZastepcaEmployeeId,
    DateOnly OdKiedy,
    DateOnly DoKiedy,
    string? Powod) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class WyznaczZastepstwoHandler(
    IZastepstwoRepository zastepstwa,
    IEmployeeRepository pracownicy) : ICommandHandler<WyznaczZastepstwoCommand, Guid>
{
    public async Task<Result<Guid>> Handle(WyznaczZastepstwoCommand request, CancellationToken cancellationToken)
    {
        var zastepca = await pracownicy.GetByIdAsync(request.ZastepcaEmployeeId, cancellationToken);
        if (zastepca is null)
        {
            return Result.Failure<Guid>(Error.NotFound(
                "Zastepstwo.ZastepcaNieIstnieje", "Wskazana osoba nie istnieje."));
        }

        // Zastepstwo, ktore nachodzi na inne, dawaloby dwoch akceptantow na ten sam dzien
        // i rozstrzygalaby o tym kolejnosc w bazie. Odrzucamy zamiast zgadywac.
        var istniejace = await zastepstwa.PobierzDlaOsobyAsync(request.ZastepowanyEmployeeId, cancellationToken);
        if (istniejace.Any(z => z.NakladaSieZ(request.OdKiedy, request.DoKiedy)))
        {
            return Result.Failure<Guid>(new Error(
                "Zastepstwo.Nakladanie",
                "W tym okresie masz już wyznaczone zastępstwo. Odwołaj je najpierw."));
        }

        var wynik = Zastepstwo.Utworz(
            request.TenantId,
            request.ZastepowanyEmployeeId,
            request.ZastepcaEmployeeId,
            request.OdKiedy,
            request.DoKiedy,
            request.Powod);

        if (wynik.IsFailure) return Result.Failure<Guid>(wynik.Error);

        await zastepstwa.DodajAsync(wynik.Value, cancellationToken);
        return wynik.Value.Id;
    }
}

/// <param name="TylkoWlasneOsoby">
/// Gdy podane, zastepstwo musi nalezec do tej osoby. Null oznacza administratora, ktory moze
/// odwolac cudze — bo ktos musi umiec odblokowac zespol, gdy kierownik zniknal bez uprzedzenia.
/// </param>
public sealed record OdwolajZastepstwoCommand(Guid Id, Guid? TylkoWlasneOsoby = null)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class OdwolajZastepstwoHandler(IZastepstwoRepository zastepstwa)
    : ICommandHandler<OdwolajZastepstwoCommand>
{
    public async Task<Result> Handle(OdwolajZastepstwoCommand request, CancellationToken cancellationToken)
    {
        var zastepstwo = await zastepstwa.PobierzAsync(request.Id, cancellationToken);
        if (zastepstwo is null)
            return Result.Failure(Error.NotFound("Zastepstwo.NieZnaleziono", "Nie znaleziono zastępstwa."));

        if (request.TylkoWlasneOsoby is Guid osoba && zastepstwo.ZastepowanyEmployeeId != osoba)
        {
            return Result.Failure(new Error(
                "Zastepstwo.NieTwoje", "Odwołać zastępstwo może osoba, która je wyznaczyła, albo administrator."));
        }

        // Odwolanie nie kasuje wiersza: wnioski juz skierowane do zastepcy maja zostac przy nim,
        // a historia „kto akceptowal w zastepstwie" musi sie zgadzac przy pozniejszym audycie.
        zastepstwo.Odwolaj();
        return Result.Success();
    }
}

public sealed record PobierzZastepstwaQuery(Guid EmployeeId) : IQuery<List<ZastepstwoDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzZastepstwaHandler(
    IZastepstwoRepository zastepstwa,
    IEmployeeRepository pracownicy) : IQueryHandler<PobierzZastepstwaQuery, List<ZastepstwoDto>>
{
    public async Task<Result<List<ZastepstwoDto>>> Handle(
        PobierzZastepstwaQuery request, CancellationToken cancellationToken)
    {
        var lista = await zastepstwa.PobierzDlaOsobyAsync(request.EmployeeId, cancellationToken);
        var dzis = DateOnly.FromDateTime(DateTime.UtcNow);

        var wynik = new List<ZastepstwoDto>();
        foreach (var z in lista)
        {
            var osoba = await pracownicy.GetByIdAsync(z.ZastepcaEmployeeId, cancellationToken);
            wynik.Add(new ZastepstwoDto(
                z.Id,
                z.ZastepowanyEmployeeId,
                z.ZastepcaEmployeeId,
                osoba is null ? "(nie znaleziono)" : $"{osoba.FirstName} {osoba.LastName}",
                z.OdKiedy,
                z.DoKiedy,
                z.Powod,
                z.ObowiazujeW(dzis)));
        }

        return wynik;
    }
}
