using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// Pracownik nie moze czytac danych innego pracownika przez podmiane identyfikatora w adresie.
/// </summary>
/// <remarks>
/// Audyt wykryl szesc endpointow, ktore braly employeeId wprost ze sciezki i oddawaly dane
/// bez sprawdzenia, czyje one sa. Wymagaly jedynie uprawnien time.view / leave.view, ktore ma
/// KAZDY pracownik, wiec wystarczylo podmienic identyfikator w adresie, zeby zobaczyc karte
/// czasu pracy, grafik albo wnioski urlopowe kolegi (razem z polem "powod").
///
/// Pierwsza wersja tego testu miala recznie wpisana liste tych szesciu sciezek — czyli
/// pokrywala dokladnie to, co juz raz peklo, i zero tego, co dopiero powstanie. Teraz trasy
/// wyliczamy z <see cref="EndpointDataSource"/> i KAZDA z identyfikatorem w adresie musi byc
/// sklasyfikowana w jednej z dwoch list ponizej. Nowy endpoint bez wpisu oblewa test.
///
/// Ograniczenie, swiadome: sprawdzamy tylko metode GET. Zadania zapisu wymagaja poprawnego
/// ciala zadania, wiec bez niego zwracaja 400 i nie da sie z tego wyczytac, czy straznik
/// zakresu w ogole tam stoi. Zapisy pilnuja osobne testy przy konkretnych komendach.
/// </remarks>
[Collection("Integration")]
public class ZakresDanychPracownikaTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Uprawnienia szeregowego pracownika: same "view", zadnego "-team" ani "-all".
    /// Wlasnie taki zestaw mial kazdy zalogowany, gdy szesc endpointow oddawalo cudze dane.
    /// </summary>
    private static readonly string[] UprawnieniaSzeregowego =
    [
        "time.view", "leave.view", "tasks.view", "workflow.view",
        "org.view", "documents.view", "dashboard.view", "identity.view",
    ];

    private readonly WorkBaseWebFactory _factory;

    public ZakresDanychPracownikaTests(WorkBaseWebFactory factory) => _factory = factory;

    /// <summary>
    /// Trasy, w ktorych parametr adresu to identyfikator PRACOWNIKA. Kazda z nich musi
    /// odpowiedziec 403, gdy podstawic identyfikator kogos innego.
    /// </summary>
    private static readonly string[] TrasyPracownika =
    [
        "/api/time/timesheet/{id}",
        "/api/time/status/{id}",
        "/api/time/break-availability/{id}",
        "/api/time/schedules/{id}",
        "/api/time/corrections/{id}",
        "/api/workspace/my-day/{id}",
        // Ponizsze trzy mialy straznika, ale nie bylo ich w recznej liscie poprzedniej wersji
        // testu — czyli nikt nie pilnowal, zeby go nie zgubily.
        "/api/leave/balances/{id}",
        "/api/leave/requests/{id}",
        "/api/workflow/approvals/pending/{id}",
        // Zastepstwa: parametr to osoba zastepowana. Cudze zastepstwa widzi tylko org.manage —
        // kto kogo zastepuje w akceptacji, to informacja o strukturze decyzyjnej firmy.
        "/api/org/zastepstwa/{id}",
    ];

    public static TheoryData<string> TrasyZDanymiPracownika() => [.. TrasyPracownika];

    /// <summary>
    /// Trasy z identyfikatorem w adresie, ktory NIE wskazuje pracownika — wraz z powodem.
    /// Powod jest obowiazkowy: bez niego lista po roku zamienia sie w zbior wyjatkow,
    /// ktorych nikt nie umie uzasadnic.
    /// </summary>
    private static readonly Dictionary<string, string> TrasyBezIdentyfikatoraPracownika = new()
    {
        // Identyfikator wskazuje rzecz, nie osobe. Widocznosc pilnuje filtr najemcy i uprawnienie.
        ["/api/activity/entity/{id}/{id}"] = "typ i identyfikator encji",
        ["/api/config/card-sections/{id}"] = "identyfikator sekcji karty",
        ["/api/config/departments/{id}"] = "identyfikator jednostki organizacyjnej",
        ["/api/dashboard/reports/{id}"] = "identyfikator definicji raportu",
        ["/api/documents/{id}/download"] = "identyfikator dokumentu",
        ["/api/documents/audit/entity/{id}/{id}"] = "typ i identyfikator encji",
        ["/api/iam/feature-flags/tenant/{id}"] = "identyfikator najemcy",
        ["/api/iam/roles/{id}"] = "identyfikator roli",
        ["/api/iam/roles/{id}/permissions"] = "identyfikator roli",
        ["/api/iam/roles/{id}/users"] = "identyfikator roli",
        ["/api/onboarding/{id}/status"] = "identyfikator zgloszenia rejestracyjnego (endpoint bez logowania)",
        ["/api/org/employees/by-number/{id}"] = "numer identyfikacyjny pracownika, nie GUID; katalog firmowy jak nizej",
        ["/api/org/units/{id}"] = "identyfikator jednostki organizacyjnej",
        ["/api/tasks/{id}"] = "identyfikator zadania",
        ["/api/tasks/{id}/attachments"] = "identyfikator zadania",
        ["/api/tasks/{id}/attachments/{id}/download"] = "identyfikator zadania i zalacznika",
        ["/api/tasks/{id}/comments"] = "identyfikator zadania",
        ["/api/time/org-unit-schedules/{id}"] = "identyfikator grafiku jednostki",
        ["/api/views/{id}"] = "nazwa typu encji (tekst), zapisane widoki filtruje sie po koncie pytajacego",
        ["/api/workflow/approvals/{id}"] = "identyfikator wniosku akceptacyjnego",
        ["/api/workflow/definitions/{id}"] = "identyfikator definicji obiegu",
        ["/api/workflow/definitions/{id}/versions"] = "identyfikator definicji obiegu",
        ["/api/workflow/instances/{id}"] = "identyfikator instancji obiegu",
        ["/api/workflow/instances/{id}/branches"] = "identyfikator instancji obiegu",
        ["/api/workflow/instances/{id}/steps"] = "identyfikator instancji obiegu",
        ["/openapi/{id}.json"] = "nazwa dokumentu OpenAPI",

        // Identyfikator KONTA (claim sub), nie pracownika — test podstawia identyfikator
        // pracownika, wiec nie da sie tu sprawdzic wariantu "dane wlasne".
        ["/api/dashboard/configs/{id}"] = "identyfikator konta; endpoint dopuszcza wylacznie wlasne (403 dla cudzego)",
        ["/api/iam/users/{id}/roles"] = "identyfikator konta; wymaga identity.view, ktorego rola Pracownik nie ma",
        ["/api/documents/audit/user/{id}"] = "identyfikator konta; caly modul dokumentow jest firmowy dla documents.view, "
                                           + "wiec filtr po autorze nie odslania nic ponad zwykla liste",

        // Identyfikator pracownika, ale dostep jest CELOWO szerszy: to katalog firmowy,
        // widoczny dla kazdego z org.view. Dane wrazliwe sa z niego usuwane osobno —
        // stawke godzinowa zeruje EmployeeEndpoints, gdy pytajacy nie ma payroll.view-team.
        ["/api/org/employees/{id}"] = "katalog firmowy (org.view); stawka zerowana bez payroll.view-team",
        ["/api/org/employees/{id}/access-status"] = "katalog firmowy (org.view); zwraca sam status konta",
    };

    /// <summary>
    /// Bramka kompletnosci. Kazda trasa GET z parametrem w adresie musi trafic do jednej
    /// z dwoch list powyzej. To jest ten warunek, ktorego brakowalo: dzieki niemu endpoint
    /// dodany jutro nie przejdzie niezauwazony.
    /// </summary>
    [Fact]
    public void Kazda_trasa_z_identyfikatorem_jest_sklasyfikowana()
    {
        var wszystkie = ZbierzTrasyGetZParametrem();

        // Gdyby wyliczanie tras przestalo dzialac, test przechodzilby na pustym zbiorze
        // i cicho stracil sens.
        Assert.NotEmpty(wszystkie);

        var sklasyfikowane = TrasyPracownika
            .Concat(TrasyBezIdentyfikatoraPracownika.Keys)
            .ToHashSet();

        var nieznane = wszystkie.Where(trasa => !sklasyfikowane.Contains(trasa)).Order().ToList();

        Assert.True(nieznane.Count == 0,
            "Trasy z identyfikatorem w adresie bez klasyfikacji zakresu danych. Dopisz kazda do " +
            $"TrasyZDanychPracownika (jesli parametr to pracownik) albo do " +
            $"TrasyBezIdentyfikatoraPracownika z powodem:{Environment.NewLine}" +
            string.Join(Environment.NewLine, nieznane));
    }

    [Theory]
    [MemberData(nameof(TrasyZDanymiPracownika))]
    public async Task Pracownik_nie_odczyta_danych_kolegi(string wzorzec)
    {
        var ja = Guid.NewGuid();
        var kolega = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            permissions: UprawnieniaSzeregowego,
            employeeId: ja);

        var response = await client.GetAsync(PodstawIdentyfikator(wzorzec, kolega));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(TrasyZDanymiPracownika))]
    public async Task Pracownik_odczyta_dane_wlasne(string wzorzec)
    {
        var ja = Guid.NewGuid();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            permissions: UprawnieniaSzeregowego,
            employeeId: ja);

        var response = await client.GetAsync(PodstawIdentyfikator(wzorzec, ja));

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Stawka godzinowa kolegi nie moze wyjsc z katalogu firmowego.
    /// </summary>
    /// <remarks>
    /// EmployeeDto niesie HourlyRate, a wszystkie trzy odczyty pracownika wymagaly jedynie
    /// org.view — uprawnienia, ktore ma rola "Pracownik". Na produkcji oznaczalo to stawki
    /// 42 osob widoczne dla kazdego zalogowanego. Karta pracownika i lista zostaja dostepne
    /// (to katalog firmowy), znika z nich tylko stawka.
    /// </remarks>
    [Fact]
    public async Task Pracownik_nie_widzi_stawki_kolegi_ani_na_karcie_ani_na_liscie()
    {
        var (ja, kolega) = await ZasiejPracownikowZeStawkami();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            // Rola "Pracownik" ma payroll.view (wlasne rozliczenie), ale NIE payroll.view-team.
            permissions: [.. UprawnieniaSzeregowego, "payroll.view"],
            employeeId: ja);

        var karta = await client.GetFromJsonAsync<JsonElement>($"/api/org/employees/{kolega}");
        Assert.Equal(JsonValueKind.Null, karta.GetProperty("hourlyRate").ValueKind);

        var wlasnaKarta = await client.GetFromJsonAsync<JsonElement>($"/api/org/employees/{ja}");
        Assert.Equal(55m, wlasnaKarta.GetProperty("hourlyRate").GetDecimal());

        var lista = await client.GetFromJsonAsync<JsonElement>("/api/org/employees?pageSize=50");
        var stawki = lista.GetProperty("items").EnumerateArray()
            .ToDictionary(
                pozycja => pozycja.GetProperty("id").GetGuid(),
                pozycja => pozycja.GetProperty("hourlyRate").ValueKind);

        Assert.Equal(JsonValueKind.Null, stawki[kolega]);
        Assert.NotEqual(JsonValueKind.Null, stawki[ja]);
    }

    /// <summary>
    /// Kontrola pozytywna: stawka kolegi JEST w bazie i da sie ja odczytac — chowa ja redakcja,
    /// a nie brak danych. Bez tego test wyzej przechodzilby rowniez wtedy, gdyby zapis stawki
    /// przestal dzialac albo gdyby pole znikalo wszystkim.
    /// </summary>
    /// <remarks>
    /// Wariant "kierownik z payroll.view-team widzi stawke zespolu" celowo nie jest tutaj:
    /// wymaga zasianych zakresow danych, a sama decyzja zakresu ma juz wlasne testy
    /// (EmployeeScopeResolverTests, DataScopeSqlTranslationTests). Tu pilnujemy tylko tego,
    /// czego wczesniej nie pilnowal nikt: ze stawka nie wychodzi do osoby postronnej.
    /// </remarks>
    [Fact]
    public async Task Stawka_ukryta_przed_kolega_jest_widoczna_dla_wlasciciela()
    {
        var (_, kolega) = await ZasiejPracownikowZeStawkami();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            permissions: [.. UprawnieniaSzeregowego, "payroll.view"],
            employeeId: kolega);

        var karta = await client.GetFromJsonAsync<JsonElement>($"/api/org/employees/{kolega}");

        Assert.Equal(70m, karta.GetProperty("hourlyRate").GetDecimal());
    }

    /// <summary>
    /// Zaklada dwoch pracownikow ze stawkami przez API kadrowe — tak jak zrobilby to
    /// administrator. Zasilanie bazy wprost z kontenera aplikacji nie dziala: fabryka testowa
    /// podmienia dostawce EF na InMemory tylko dla zadan, a w korzeniu kontenera zostaje
    /// rowniez Npgsql i rozstrzygniecie DbContext konczy sie wyjatkiem o dwoch dostawcach.
    /// </summary>
    private async Task<(Guid Ja, Guid Kolega)> ZasiejPracownikowZeStawkami()
    {
        using var kadry = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            tenantId: TenantId,
            permissions: ["org.view", "org.create", "org.edit"],
            employeeId: Guid.NewGuid());

        var ja = await ZalozPracownika(kadry, "Ja", 55m);
        var kolega = await ZalozPracownika(kadry, "Kolega", 70m);

        return (ja, kolega);
    }

    private static async Task<Guid> ZalozPracownika(HttpClient kadry, string imie, decimal stawka)
    {
        var utworzenie = await kadry.PostAsJsonAsync("/api/org/employees", new
        {
            firstName = imie,
            lastName = "Testowy",
            email = $"{imie.ToLowerInvariant()}-{Guid.NewGuid():N}@firma.pl",
            employeeNumber = (string?)null,
            hireDate = DateTime.UtcNow,
        });
        await Udalo(utworzenie, "utworzenie pracownika");

        var id = await utworzenie.Content.ReadFromJsonAsync<Guid>();

        var stawkaOdpowiedz = await kadry.PutAsJsonAsync(
            $"/api/org/employees/{id}/hourly-rate", new { hourlyRate = stawka });
        await Udalo(stawkaOdpowiedz, $"ustawienie stawki pracownikowi {id}");

        return id;
    }

    /// <summary>Asercja z trescia odpowiedzi — samo EnsureSuccessStatusCode nie mowi, co poszlo zle.</summary>
    private static async Task Udalo(HttpResponseMessage odpowiedz, string krok)
    {
        if (odpowiedz.IsSuccessStatusCode) return;

        var tresc = await odpowiedz.Content.ReadAsStringAsync();
        Assert.Fail($"{krok}: {(int)odpowiedz.StatusCode} {odpowiedz.StatusCode}. Tresc: {tresc}");
    }

    private static string PodstawIdentyfikator(string wzorzec, Guid identyfikator)
    {
        var wynik = wzorzec;
        var otwarcie = wynik.IndexOf('{');
        while (otwarcie >= 0)
        {
            var zamkniecie = wynik.IndexOf('}', otwarcie);
            if (zamkniecie < 0) break;
            wynik = wynik[..otwarcie] + identyfikator + wynik[(zamkniecie + 1)..];
            otwarcie = wynik.IndexOf('{');
        }

        return wynik;
    }

    /// <summary>
    /// Zwraca wzorce tras GET zawierajace parametr, w postaci znormalizowanej do "{id}" —
    /// nazwa parametru i jego ograniczenie ("{id:guid}") nie maja tu znaczenia, a bez
    /// normalizacji ta sama trasa raz po raz wracalaby jako "nowa".
    /// </summary>
    private List<string> ZbierzTrasyGetZParametrem()
    {
        var zrodlo = _factory.Services.GetRequiredService<EndpointDataSource>();

        return zrodlo.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .GetMetadata<HttpMethodMetadata>()
                ?.HttpMethods.Contains(HttpMethods.Get) == true)
            .Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)
            .Where(trasa => trasa.Contains('{'))
            .Select(ZnormalizujParametry)
            .Distinct()
            .ToList();
    }

    private static string ZnormalizujParametry(string trasa)
    {
        var wynik = trasa;
        var otwarcie = wynik.IndexOf('{');
        while (otwarcie >= 0)
        {
            var zamkniecie = wynik.IndexOf('}', otwarcie);
            if (zamkniecie < 0) break;
            wynik = wynik[..otwarcie] + "{id}" + wynik[(zamkniecie + 1)..];
            otwarcie = wynik.IndexOf('{', otwarcie + 4);
        }

        return wynik.StartsWith('/') ? wynik : "/" + wynik;
    }
}
