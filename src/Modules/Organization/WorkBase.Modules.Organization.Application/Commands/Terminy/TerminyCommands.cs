using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Terminy;

public sealed record TypTerminuDto(
    Guid Id, string Kod, string Nazwa, string? Opis, int DniOstrzezenia, bool Aktywny);

public sealed record TerminDto(
    Guid Id,
    Guid EmployeeId,
    Guid TypTerminuId,
    string TypNazwa,
    DateOnly WaznyDo,
    DateOnly? WykonanyDnia,
    string? Notatka,
    Guid? DokumentId,
    bool Archiwalny,
    /// <summary>Aktualny, Zbliza albo Minal — liczone na bieżąco, nie przechowywane.</summary>
    string Stan,
    int DniDoTerminu);

public sealed record WygasajacyTerminDto(
    Guid Id,
    Guid EmployeeId,
    string ImieNazwisko,
    string TypNazwa,
    DateOnly WaznyDo,
    string Stan,
    int DniDoTerminu);

// ---------------------------------------------------------------- rodzaje terminów

public sealed record ZapiszTypTerminuCommand(
    Guid? Id, string Kod, string Nazwa, string? Opis, int DniOstrzezenia, bool Aktywny)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZapiszTypTerminuHandler(ITypTerminuRepository typy)
    : ICommandHandler<ZapiszTypTerminuCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ZapiszTypTerminuCommand request, CancellationToken ct)
    {
        if (await typy.KodIstniejeAsync(request.Kod, request.Id, ct))
        {
            return Result.Failure<Guid>(Error.Conflict(
                "TypTerminu.KodZajety", $"Rodzaj o kodzie „{request.Kod}” już istnieje."));
        }

        try
        {
            if (request.Id is not Guid id)
            {
                var nowy = TypTerminu.Utworz(
                    request.TenantId, request.Kod, request.Nazwa, request.Opis, request.DniOstrzezenia);
                await typy.DodajAsync(nowy, ct);
                return nowy.Id;
            }

            var istniejacy = await typy.PobierzAsync(id, ct);
            if (istniejacy is null)
                return Result.Failure<Guid>(Error.NotFound("TypTerminu.NieIstnieje", "Rodzaj nie istnieje."));

            istniejacy.Zmien(request.Nazwa, request.Opis, request.DniOstrzezenia, request.Aktywny);
            return istniejacy.Id;
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(Error.Validation("TypTerminu.Niepoprawny", ex.Message));
        }
    }
}

// ---------------------------------------------------------------- terminy pracowników

public sealed record ZapiszTerminCommand(
    Guid? Id,
    Guid EmployeeId,
    Guid TypTerminuId,
    DateOnly WaznyDo,
    DateOnly? WykonanyDnia,
    string? Notatka,
    Guid? DokumentId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZapiszTerminHandler(
    ITerminPracownikaRepository terminy,
    ITypTerminuRepository typy,
    IEmployeeRepository pracownicy) : ICommandHandler<ZapiszTerminCommand, Guid>
{
    public async Task<Result<Guid>> Handle(ZapiszTerminCommand request, CancellationToken ct)
    {
        try
        {
            if (request.Id is Guid id)
            {
                var istniejacy = await terminy.PobierzAsync(id, ct);
                if (istniejacy is null)
                    return Result.Failure<Guid>(Error.NotFound("Termin.NieIstnieje", "Termin nie istnieje."));

                istniejacy.Zmien(request.WaznyDo, request.WykonanyDnia, request.Notatka, request.DokumentId);
                return istniejacy.Id;
            }

            if (!await pracownicy.ExistsAsync(request.EmployeeId, ct))
                return Result.Failure<Guid>(Error.NotFound("Termin.PracownikNieIstnieje", "Pracownik nie istnieje."));

            if (await typy.PobierzAsync(request.TypTerminuId, ct) is null)
                return Result.Failure<Guid>(Error.NotFound("Termin.TypNieIstnieje", "Rodzaj terminu nie istnieje."));

            var nowy = TerminPracownika.Utworz(
                request.TenantId, request.EmployeeId, request.TypTerminuId,
                request.WaznyDo, request.WykonanyDnia, request.Notatka, request.DokumentId);
            await terminy.DodajAsync(nowy, ct);
            return nowy.Id;
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(Error.Validation("Termin.Niepoprawny", ex.Message));
        }
    }
}

/// <summary>
/// Odnowienie: stary termin trafia do archiwum, nowy powstaje obok. Nie nadpisujemy daty,
/// bo historia badań i szkoleń bywa potrzebna przy kontroli.
/// </summary>
public sealed record OdnowTerminCommand(
    Guid Id, DateOnly NowyWaznyDo, DateOnly? WykonanyDnia, string? Notatka)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class OdnowTerminHandler(ITerminPracownikaRepository terminy)
    : ICommandHandler<OdnowTerminCommand, Guid>
{
    public async Task<Result<Guid>> Handle(OdnowTerminCommand request, CancellationToken ct)
    {
        var stary = await terminy.PobierzAsync(request.Id, ct);
        if (stary is null)
            return Result.Failure<Guid>(Error.NotFound("Termin.NieIstnieje", "Termin nie istnieje."));

        try
        {
            var nowy = TerminPracownika.Utworz(
                stary.TenantId, stary.EmployeeId, stary.TypTerminuId,
                request.NowyWaznyDo, request.WykonanyDnia, request.Notatka, dokumentId: null);
            await terminy.DodajAsync(nowy, ct);
            stary.Zarchiwizuj();
            return nowy.Id;
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(Error.Validation("Termin.Niepoprawny", ex.Message));
        }
    }
}

public sealed record ZarchiwizujTerminCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZarchiwizujTerminHandler(ITerminPracownikaRepository terminy)
    : ICommandHandler<ZarchiwizujTerminCommand>
{
    public async Task<Result> Handle(ZarchiwizujTerminCommand request, CancellationToken ct)
    {
        var termin = await terminy.PobierzAsync(request.Id, ct);
        if (termin is null)
            return Result.Failure(Error.NotFound("Termin.NieIstnieje", "Termin nie istnieje."));

        termin.Zarchiwizuj();
        return Result.Success();
    }
}
