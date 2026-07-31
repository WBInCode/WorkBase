using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Shared.Auth;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Pilnuje spojnosci miedzy uprawnieniami wymaganymi przez endpointy a slownikiem uprawnien.
/// </summary>
/// <remarks>
/// Audyt produkcji wykazal 20 endpointow wymagajacych uprawnien, ktorych w ogole nie bylo
/// w slowniku (ai.use, sales.manage, forms.manage, cases.assign, cases.comment,
/// contacts.assign, tasks.comment). Takiego uprawnienia nie da sie nikomu nadac, wiec
/// endpoint odrzucal kazdego — rowniez Super Admina — i wygladalo to jak blad uprawnien.
/// Zwykle testy tego nie lapaly, bo sprawdzaly zachowanie przy nadanym uprawnieniu.
/// </remarks>
[Collection("Integration")]
public class PermissionCatalogTests
{
    private readonly WorkBaseWebFactory _factory;

    public PermissionCatalogTests(WorkBaseWebFactory factory) => _factory = factory;

    [Fact]
    public void Kazde_uprawnienie_wymagane_przez_endpoint_istnieje_w_slowniku()
    {
        var wymagane = ZbierzWymaganeUprawnienia();

        // Gdyby metadane przestaly sie zapisywac, test przechodzilby na pustym zbiorze
        // i cicho stracil sens — dlatego najpierw sprawdzamy, ze cokolwiek zebral.
        Assert.NotEmpty(wymagane);

        var brakujace = wymagane.Where(kod => !IamSeeder.AllPermissionCodes.Contains(kod)).Order().ToList();

        Assert.True(brakujace.Count == 0,
            $"Endpointy wymagaja uprawnien, ktorych nie ma w IamSeeder, wiec sa niedostepne dla " +
            $"kazdego uzytkownika: {string.Join(", ", brakujace)}");
    }

    [Fact]
    public void Slownik_uprawnien_pokrywa_wszystkie_moduly_z_katalogu()
    {
        // Kazdy modul dostaje w seederze komplet CRUD, wiec brak "modul.view" oznacza,
        // ze modul dodano do katalogu, ale zapomniano o uprawnieniach.
        var bezUprawnien = WorkBase.Shared.Modules.ModuleCatalog.All
            .Select(modul => modul.Key)
            .Where(klucz => !IamSeeder.AllPermissionCodes.Contains($"{klucz}.view"))
            .Order()
            .ToList();

        Assert.True(bezUprawnien.Count == 0,
            $"Moduly z katalogu bez uprawnien w IamSeeder: {string.Join(", ", bezUprawnien)}");
    }

    private HashSet<string> ZbierzWymaganeUprawnienia()
    {
        var zrodlo = _factory.Services.GetRequiredService<EndpointDataSource>();

        return zrodlo.Endpoints
            .SelectMany(endpoint => endpoint.Metadata.GetOrderedMetadata<RequiredPermissionsMetadata>())
            .SelectMany(metadata => metadata.Permissions)
            .ToHashSet();
    }
}
