using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Services;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DodajDzienWolnyCommand(
    DateOnly Data,
    string Nazwa,
    RodzajDniaWolnego Rodzaj,
    bool ObnizaNorme) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DodajDzienWolnyHandler(IDzienWolnyRepository dni)
    : ICommandHandler<DodajDzienWolnyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(DodajDzienWolnyCommand request, CancellationToken ct)
    {
        if (await dni.IstniejeWDniuAsync(request.TenantId, request.Data, ct))
        {
            return Result.Failure<Guid>(new Error(
                "DzienWolny.JuzIstnieje", "Ten dzień jest już oznaczony jako wolny."));
        }

        var wynik = DzienWolny.Utworz(
            request.TenantId, request.Data, request.Nazwa, request.Rodzaj, request.ObnizaNorme);
        if (wynik.IsFailure) return Result.Failure<Guid>(wynik.Error);

        await dni.DodajAsync(wynik.Value, ct);
        return wynik.Value.Id;
    }
}

public sealed record ZmienDzienWolnyCommand(
    Guid Id,
    string Nazwa,
    RodzajDniaWolnego Rodzaj,
    bool ObnizaNorme) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class ZmienDzienWolnyHandler(IDzienWolnyRepository dni)
    : ICommandHandler<ZmienDzienWolnyCommand>
{
    public async Task<Result> Handle(ZmienDzienWolnyCommand request, CancellationToken ct)
    {
        var dzien = await dni.PobierzAsync(request.TenantId, request.Id, ct);
        if (dzien is null)
            return Result.Failure(Error.NotFound("DzienWolny.NieZnaleziono", "Nie znaleziono dnia wolnego."));

        dzien.Zmien(request.Nazwa, request.Rodzaj, request.ObnizaNorme);
        return Result.Success();
    }
}

public sealed record UsunDzienWolnyCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UsunDzienWolnyHandler(IDzienWolnyRepository dni)
    : ICommandHandler<UsunDzienWolnyCommand>
{
    public async Task<Result> Handle(UsunDzienWolnyCommand request, CancellationToken ct)
    {
        var dzien = await dni.PobierzAsync(request.TenantId, request.Id, ct);
        if (dzien is null)
            return Result.Failure(Error.NotFound("DzienWolny.NieZnaleziono", "Nie znaleziono dnia wolnego."));

        dni.Usun(dzien);
        return Result.Success();
    }
}

public sealed record WstawZestawPolskiCommand(int Rok) : ICommand<int>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Wstawia typowe polskie dni wolne, pomijajac daty juz wpisane.
/// </summary>
/// <remarks>
/// Pomijanie istniejacych jest istotne: administrator moze wolac to wielokrotnie (np. po
/// dopisaniu wlasnych dni firmowych), a nadpisanie skasowaloby jego zmiany. Zwraca liczbe
/// faktycznie dodanych, zeby ekran mogl powiedziec, co sie stalo.
/// </remarks>
public sealed class WstawZestawPolskiHandler(IDzienWolnyRepository dni)
    : ICommandHandler<WstawZestawPolskiCommand, int>
{
    public async Task<Result<int>> Handle(WstawZestawPolskiCommand request, CancellationToken ct)
    {
        var istniejace = (await dni.PobierzZakresAsync(
                request.TenantId, new DateOnly(request.Rok, 1, 1), new DateOnly(request.Rok, 12, 31), ct))
            .Select(d => d.Data)
            .ToHashSet();

        var dodane = 0;
        foreach (var propozycja in KalendarzPolski.ProponowaneDniWolne(request.Rok))
        {
            if (istniejace.Contains(propozycja.Data)) continue;

            var wynik = DzienWolny.Utworz(
                request.TenantId, propozycja.Data, propozycja.Nazwa, RodzajDniaWolnego.Swieto);
            if (wynik.IsFailure) continue;

            await dni.DodajAsync(wynik.Value, ct);
            dodane++;
        }

        return dodane;
    }
}

public sealed record PobierzDniWolneQuery(int Rok) : IQuery<List<DzienWolnyDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record DzienWolnyDto(
    Guid Id, DateOnly Data, string Nazwa, string Rodzaj, bool ObnizaNorme);

public sealed class PobierzDniWolneHandler(IDzienWolnyRepository dni)
    : IQueryHandler<PobierzDniWolneQuery, List<DzienWolnyDto>>
{
    public async Task<Result<List<DzienWolnyDto>>> Handle(PobierzDniWolneQuery request, CancellationToken ct)
    {
        var lista = await dni.PobierzZakresAsync(
            request.TenantId, new DateOnly(request.Rok, 1, 1), new DateOnly(request.Rok, 12, 31), ct);

        return lista
            .Select(d => new DzienWolnyDto(d.Id, d.Data, d.Nazwa, d.Rodzaj.ToString(), d.ObnizaNorme))
            .ToList();
    }
}
