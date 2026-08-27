using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Organization.Application.Commands.Mienie;

public sealed record MienieDto(
    Guid Id,
    Guid EmployeeId,
    string Rodzaj,
    string Nazwa,
    string? NumerSeryjny,
    decimal? Wartosc,
    DateOnly WydanoDnia,
    DateOnly? ZwroconoDnia,
    DateTime? PotwierdzonoOdbior,
    string? Notatka);

public sealed record MienieDoZwrotuDto(
    Guid Id,
    Guid EmployeeId,
    string ImieNazwisko,
    /// <summary>„Nieaktywny" albo data odejścia — po co ta osoba jest na liście.</summary>
    string Powod,
    string Rodzaj,
    string Nazwa,
    string? NumerSeryjny,
    decimal? Wartosc,
    DateOnly WydanoDnia);

internal static class MienieMapowanie
{
    public static MienieDto DoDto(this MieniePowierzone m) => new(
        m.Id, m.EmployeeId, m.Rodzaj, m.Nazwa, m.NumerSeryjny, m.Wartosc,
        m.WydanoDnia, m.ZwroconoDnia, m.PotwierdzonoOdbior, m.Notatka);
}

// ---------------------------------------------------------------- wydanie i zmiana

public sealed record WydajMienieCommand(
    Guid? Id,
    Guid EmployeeId,
    string Rodzaj,
    string Nazwa,
    DateOnly WydanoDnia,
    string? NumerSeryjny,
    decimal? Wartosc,
    string? Notatka) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class WydajMienieHandler(
    IMieniePowierzoneRepository mienie,
    IEmployeeRepository pracownicy) : ICommandHandler<WydajMienieCommand, Guid>
{
    public async Task<Result<Guid>> Handle(WydajMienieCommand request, CancellationToken ct)
    {
        try
        {
            if (request.Id is Guid id)
            {
                var istniejace = await mienie.PobierzAsync(id, ct);
                if (istniejace is null)
                    return Result.Failure<Guid>(Error.NotFound("Mienie.NieIstnieje", "Wpis nie istnieje."));

                istniejace.Zmien(request.Rodzaj, request.Nazwa, request.NumerSeryjny, request.Wartosc, request.Notatka);
                return istniejace.Id;
            }

            if (!await pracownicy.ExistsAsync(request.EmployeeId, ct))
                return Result.Failure<Guid>(Error.NotFound("Mienie.PracownikNieIstnieje", "Pracownik nie istnieje."));

            var nowe = MieniePowierzone.Wydaj(
                request.TenantId, request.EmployeeId, request.Rodzaj, request.Nazwa,
                request.WydanoDnia, request.NumerSeryjny, request.Wartosc, request.Notatka);
            await mienie.DodajAsync(nowe, ct);
            return nowe.Id;
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(Error.Validation("Mienie.Niepoprawne", ex.Message));
        }
    }
}

// ---------------------------------------------------------------- zwrot

public sealed record ZwrocMienieCommand(Guid Id, DateOnly ZwroconoDnia, string? Notatka)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZwrocMienieHandler(IMieniePowierzoneRepository mienie)
    : ICommandHandler<ZwrocMienieCommand>
{
    public async Task<Result> Handle(ZwrocMienieCommand request, CancellationToken ct)
    {
        var wpis = await mienie.PobierzAsync(request.Id, ct);
        if (wpis is null)
            return Result.Failure(Error.NotFound("Mienie.NieIstnieje", "Wpis nie istnieje."));

        try
        {
            wpis.Zwroc(request.ZwroconoDnia, request.Notatka);
            return Result.Success();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result.Failure(Error.Validation("Mienie.Zwrot", ex.Message));
        }
    }
}

// ---------------------------------------------------------------- potwierdzenie odbioru

/// <summary>
/// Potwierdzenie składa pracownik we własnym imieniu. <paramref name="EmployeeIdPytajacego"/>
/// pochodzi z tokenu, nie z ciała żądania — inaczej dałoby się potwierdzić za kogoś.
/// </summary>
public sealed record PotwierdzOdbiorMieniaCommand(Guid Id, Guid EmployeeIdPytajacego)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PotwierdzOdbiorMieniaHandler(IMieniePowierzoneRepository mienie)
    : ICommandHandler<PotwierdzOdbiorMieniaCommand>
{
    public async Task<Result> Handle(PotwierdzOdbiorMieniaCommand request, CancellationToken ct)
    {
        var wpis = await mienie.PobierzAsync(request.Id, ct);
        if (wpis is null)
            return Result.Failure(Error.NotFound("Mienie.NieIstnieje", "Wpis nie istnieje."));

        // Cudzy wpis dostaje NotFound, nie Forbidden: nie zdradzamy, ze taki identyfikator istnieje.
        if (wpis.EmployeeId != request.EmployeeIdPytajacego)
            return Result.Failure(Error.NotFound("Mienie.NieIstnieje", "Wpis nie istnieje."));

        if (wpis.Zwrocone)
            return Result.Failure(Error.Validation("Mienie.JuzZwrocone", "Nie da się potwierdzić odbioru rzeczy już zwróconej."));

        wpis.PotwierdzOdbior(DateTime.UtcNow);
        return Result.Success();
    }
}

// ---------------------------------------------------------------- zapytania

public sealed record PobierzMieniePracownikaQuery(Guid EmployeeId, bool ZeZwroconymi = false)
    : IQuery<List<MienieDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzMieniePracownikaHandler(IMieniePowierzoneRepository mienie)
    : IQueryHandler<PobierzMieniePracownikaQuery, List<MienieDto>>
{
    public async Task<Result<List<MienieDto>>> Handle(PobierzMieniePracownikaQuery request, CancellationToken ct)
    {
        var lista = await mienie.PobierzDlaPracownikaAsync(request.EmployeeId, request.ZeZwroconymi, ct);
        return lista.Select(m => m.DoDto()).ToList();
    }
}

/// <summary>
/// „Co do zwrotu" — niezwrócone rzeczy u osób, które odchodzą albo już odeszły. Lista niesie
/// nazwiska, więc zawężenie zakresem danych robi endpoint — tak samo jak przy terminach.
/// </summary>
public sealed record PobierzMienieDoZwrotuQuery : IQuery<List<MienieDoZwrotuDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PobierzMienieDoZwrotuHandler(IMieniePowierzoneRepository mienie)
    : IQueryHandler<PobierzMienieDoZwrotuQuery, List<MienieDoZwrotuDto>>
{
    public async Task<Result<List<MienieDoZwrotuDto>>> Handle(PobierzMienieDoZwrotuQuery request, CancellationToken ct)
    {
        var lista = await mienie.PobierzDoZwrotuAsync(ct);
        return lista.Select(x => new MienieDoZwrotuDto(
            x.Mienie.Id,
            x.Mienie.EmployeeId,
            $"{x.Imie} {x.Nazwisko}",
            x.StatusPracownika == EmployeeStatus.Inactive
                ? "nieaktywny"
                : $"odchodzi {x.TerminationDate:dd.MM.yyyy}",
            x.Mienie.Rodzaj,
            x.Mienie.Nazwa,
            x.Mienie.NumerSeryjny,
            x.Mienie.Wartosc,
            x.Mienie.WydanoDnia)).ToList();
    }
}

public sealed record PoliczNiezwroconeQuery(Guid EmployeeId) : IQuery<int>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class PoliczNiezwroconeHandler(IMieniePowierzoneRepository mienie)
    : IQueryHandler<PoliczNiezwroconeQuery, int>
{
    public async Task<Result<int>> Handle(PoliczNiezwroconeQuery request, CancellationToken ct) =>
        await mienie.PoliczNiezwroconeAsync(request.EmployeeId, ct);
}
