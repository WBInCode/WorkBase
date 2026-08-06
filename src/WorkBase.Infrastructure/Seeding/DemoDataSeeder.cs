using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

/// <summary>
/// Dane pokazowe dla najemcy demonstracyjnego: struktura działów, 35 pracowników,
/// stawki godzinowe, grafiki i zarejestrowany czas pracy.
///
/// Uruchamiane wyłącznie z wiersza poleceń (<c>--seed-demo &lt;tenantId&gt;</c>), nigdy
/// automatycznie — to dane prezentacyjne, nie część rozruchu aplikacji.
///
/// Idempotentny: rozpoznaje istniejące jednostki po kodzie, pracowników po adresie
/// e-mail, grafiki i wejścia po dacie. Powtórne uruchomienie uzupełnia braki.
/// </summary>
public static class DemoDataSeeder
{
    private sealed record OsobaDemo(
        string Imie,
        string Nazwisko,
        string Stanowisko,
        string KodDzialu,
        bool Kierownik,
        decimal Stawka,
        string ZatrudnionyOd);

    private sealed record DzialDemo(string Kod, string Nazwa, string Rodzic);

    private const string DomenaDemo = "demo.wb-partners.pl";

    private static readonly DzialDemo[] Dzialy =
    [
        new("ZARZ", "Zarząd", "ROOT"),
        new("HR", "Kadry i Płace", "ROOT"),
        new("PROD", "Produkcja", "ROOT"),
        new("SPRZ", "Sprzedaż", "ROOT"),
        new("MAG", "Magazyn i Logistyka", "ROOT"),
        new("IT", "IT", "ROOT"),
        new("KSIE", "Księgowość", "ROOT"),
    ];

    private static readonly OsobaDemo[] Osoby =
    [
        new("Marek", "Nowak", "Prezes Zarządu", "ZARZ", true, 180m, "2015-03-02"),
        new("Anna", "Zielińska", "Dyrektor Operacyjny", "ZARZ", true, 140m, "2016-09-01"),

        new("Joanna", "Wieczorek", "Kierownik Działu HR", "HR", true, 95m, "2017-04-10"),
        new("Magdalena", "Krawczyk", "Specjalista ds. HR", "HR", false, 62m, "2019-02-18"),
        new("Ewa", "Adamczyk", "Specjalista ds. Kadr i Płac", "HR", false, 64m, "2018-11-05"),

        new("Tomasz", "Wójcik", "Kierownik Produkcji", "PROD", true, 88m, "2016-05-16"),
        new("Grzegorz", "Mazur", "Brygadzista", "PROD", false, 58m, "2018-03-12"),
        new("Rafał", "Sikora", "Operator CNC", "PROD", false, 48m, "2019-07-01"),
        new("Damian", "Baran", "Operator CNC", "PROD", false, 46m, "2020-01-13"),
        new("Sebastian", "Górski", "Operator maszyn", "PROD", false, 42m, "2020-06-22"),
        new("Łukasz", "Pawlak", "Operator maszyn", "PROD", false, 42m, "2021-02-08"),
        new("Krzysztof", "Duda", "Ślusarz", "PROD", false, 45m, "2019-10-14"),
        new("Marcin", "Sobczak", "Kontroler jakości", "PROD", false, 52m, "2018-08-27"),
        new("Paweł", "Michalak", "Technolog", "PROD", false, 66m, "2017-09-04"),

        new("Katarzyna", "Lewandowska", "Kierownik Sprzedaży", "SPRZ", true, 92m, "2017-01-09"),
        new("Bartosz", "Kowalczyk", "Przedstawiciel Handlowy", "SPRZ", false, 58m, "2019-03-25"),
        new("Natalia", "Wróbel", "Przedstawiciel Handlowy", "SPRZ", false, 56m, "2020-09-07"),
        new("Kamil", "Jankowski", "Przedstawiciel Handlowy", "SPRZ", false, 55m, "2021-05-17"),
        new("Aleksandra", "Piotrowska", "Specjalista ds. Obsługi Klienta", "SPRZ", false, 47m, "2020-11-02"),
        new("Dominika", "Nowicka", "Specjalista ds. Obsługi Klienta", "SPRZ", false, 46m, "2022-01-10"),
        new("Jakub", "Zawadzki", "Specjalista ds. Ofertowania", "SPRZ", false, 54m, "2021-08-30"),

        new("Piotr", "Kamiński", "Kierownik Magazynu", "MAG", true, 74m, "2017-06-19"),
        new("Adrian", "Szewczyk", "Magazynier", "MAG", false, 40m, "2019-04-08"),
        new("Mateusz", "Olszewski", "Magazynier", "MAG", false, 40m, "2020-02-24"),
        new("Karol", "Stępień", "Operator wózka widłowego", "MAG", false, 44m, "2018-12-03"),
        new("Wojciech", "Malinowski", "Specjalista ds. Logistyki", "MAG", false, 53m, "2019-09-16"),
        new("Sylwia", "Bąk", "Specjalista ds. Zaopatrzenia", "MAG", false, 51m, "2021-03-15"),

        new("Michał", "Dąbrowski", "Kierownik Działu IT", "IT", true, 105m, "2018-01-22"),
        new("Przemysław", "Ostrowski", "Administrator Systemów", "IT", false, 78m, "2019-11-12"),
        new("Karolina", "Sadowska", "Specjalista ds. Wsparcia IT", "IT", false, 58m, "2021-06-01"),
        new("Filip", "Wysocki", "Specjalista ds. Wsparcia IT", "IT", false, 55m, "2022-04-19"),

        new("Agnieszka", "Szymańska", "Główna Księgowa", "KSIE", true, 98m, "2016-02-15"),
        new("Beata", "Rutkowska", "Księgowa", "KSIE", false, 63m, "2018-05-21"),
        new("Monika", "Głowacka", "Księgowa", "KSIE", false, 61m, "2020-08-11"),
        new("Tomasz", "Sokołowski", "Analityk Finansowy", "KSIE", false, 72m, "2021-10-04"),
    ];

    public static async Task SeedAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        ILogger logger,
        CancellationToken ct = default)
    {
        var tenant = await db.Set<Tenant>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException($"Najemca {tenantId} nie istnieje.");
        logger.LogInformation("Dane pokazowe dla najemcy {Nazwa} ({Id})", tenant.Name, tenantId);

        var jednostki = await ZapewnijJednostkiAsync(db, tenantId, ct);
        var stanowiska = await ZapewnijStanowiskaAsync(db, tenantId, ct);
        var pracownicy = await ZapewnijPracownikowAsync(db, tenantId, jednostki, stanowiska, ct);
        await ZapewnijPrzelozonychAsync(db, tenantId, pracownicy, ct);
        var grafikow = await ZapewnijGrafikiAsync(db, tenantId, pracownicy, ct);
        var wejsc = await ZapewnijCzasPracyAsync(db, tenantId, pracownicy, ct);

        logger.LogInformation(
            "Gotowe: {Jednostki} jednostek, {Stanowiska} stanowisk, {Pracownicy} pracowników, {Grafiki} dni grafiku, {Wejscia} wpisów czasu pracy",
            jednostki.Count, stanowiska.Count, pracownicy.Count, grafikow, wejsc);
    }

    private static async Task<Dictionary<string, Guid>> ZapewnijJednostkiAsync(
        WorkBaseDbContext db, Guid tenantId, CancellationToken ct)
    {
        var istniejace = await db.Set<OrganizationUnit>().IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(ct);

        var root = istniejace.FirstOrDefault(u => u.ParentId == null)
            ?? throw new InvalidOperationException(
                "Najemca nie ma jednostki głównej — provisioning nie zakończył się poprawnie.");

        var typDzialu = await db.Set<OrganizationUnitType>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Name == "Dział", ct)
            ?? throw new InvalidOperationException("Brak typu jednostki „Dział”.");

        var mapa = new Dictionary<string, Guid> { ["ROOT"] = root.Id };

        foreach (var dzial in Dzialy)
        {
            var juz = istniejace.FirstOrDefault(u => u.Code == dzial.Kod);
            if (juz is not null) { mapa[dzial.Kod] = juz.Id; continue; }

            var jednostka = OrganizationUnit.Create(tenantId, dzial.Nazwa, dzial.Kod, typDzialu.Id, mapa[dzial.Rodzic]);
            db.Set<OrganizationUnit>().Add(jednostka);
            await db.SaveChangesAsync(ct);

            // Tabela domknięcia: własny wiersz o głębokości 0 plus ścieżki wszystkich przodków.
            db.Set<OrganizationUnitClosure>().Add(
                OrganizationUnitClosure.Create(jednostka.Id, jednostka.Id, 0));
            var przodkowie = await db.Set<OrganizationUnitClosure>()
                .Where(c => c.DescendantId == mapa[dzial.Rodzic])
                .ToListAsync(ct);
            foreach (var przodek in przodkowie)
            {
                db.Set<OrganizationUnitClosure>().Add(
                    OrganizationUnitClosure.Create(przodek.AncestorId, jednostka.Id, przodek.Depth + 1));
            }
            await db.SaveChangesAsync(ct);

            mapa[dzial.Kod] = jednostka.Id;
        }

        return mapa;
    }

    private static async Task<Dictionary<string, Guid>> ZapewnijStanowiskaAsync(
        WorkBaseDbContext db, Guid tenantId, CancellationToken ct)
    {
        var istniejace = await db.Set<Position>().IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(ct);

        var mapa = istniejace.ToDictionary(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var grupa in Osoby.GroupBy(o => o.Stanowisko))
        {
            if (mapa.ContainsKey(grupa.Key)) continue;
            var kierownicze = grupa.Any(o => o.Kierownik);
            var stanowisko = Position.Create(tenantId, grupa.Key, null, null, kierownicze);
            db.Set<Position>().Add(stanowisko);
            await db.SaveChangesAsync(ct);
            mapa[grupa.Key] = stanowisko.Id;
        }

        return mapa;
    }

    private static async Task<Dictionary<string, (Guid Id, OsobaDemo Dane)>> ZapewnijPracownikowAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        Dictionary<string, Guid> jednostki,
        Dictionary<string, Guid> stanowiska,
        CancellationToken ct)
    {
        var istniejacy = await db.Set<Employee>().IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(ct);

        var wynik = new Dictionary<string, (Guid, OsobaDemo)>();
        var numer = istniejacy.Count;

        foreach (var osoba in Osoby)
        {
            var email = Adres(osoba);
            var pracownik = istniejacy.FirstOrDefault(
                e => string.Equals(e.Email, email, StringComparison.OrdinalIgnoreCase));

            if (pracownik is null)
            {
                numer++;
                pracownik = Employee.Create(
                    tenantId, osoba.Imie, osoba.Nazwisko, email,
                    $"NI-{numer:D3}", DateTime.Parse(osoba.ZatrudnionyOd).ToUniversalTime());
                db.Set<Employee>().Add(pracownik);
            }

            pracownik.SetHourlyRate(osoba.Stawka);
            await db.SaveChangesAsync(ct);
            wynik[email] = (pracownik.Id, osoba);

            var maPrzypisanie = await db.Set<EmployeeAssignment>().IgnoreQueryFilters()
                .AnyAsync(a => a.EmployeeId == pracownik.Id && a.IsPrimary, ct);
            if (!maPrzypisanie)
            {
                db.Set<EmployeeAssignment>().Add(EmployeeAssignment.Create(
                    tenantId, pracownik.Id, jednostki[osoba.KodDzialu], stanowiska[osoba.Stanowisko],
                    true, DateTime.Parse(osoba.ZatrudnionyOd).ToUniversalTime()));
                await db.SaveChangesAsync(ct);
            }
        }

        return wynik;
    }

    /// <summary>Prezes nadzoruje kierowników, każdy kierownik swój dział.</summary>
    private static async Task ZapewnijPrzelozonychAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        Dictionary<string, (Guid Id, OsobaDemo Dane)> pracownicy,
        CancellationToken ct)
    {
        var istniejace = await db.Set<SupervisorRelation>().IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && r.EndDate == null)
            .ToListAsync(ct);

        bool Brakuje(Guid szef, Guid podwladny) =>
            !istniejace.Any(r => r.SupervisorEmployeeId == szef && r.SubordinateEmployeeId == podwladny);

        var prezes = pracownicy.Values.First(p => p.Dane.Stanowisko == "Prezes Zarządu");
        var kierownicy = pracownicy.Values
            .Where(p => p.Dane.Kierownik && p.Dane.Stanowisko != "Prezes Zarządu")
            .ToList();

        foreach (var kierownik in kierownicy)
        {
            if (Brakuje(prezes.Id, kierownik.Id))
            {
                db.Set<SupervisorRelation>().Add(SupervisorRelation.Create(
                    tenantId, prezes.Id, kierownik.Id, DateTime.UtcNow.AddYears(-1)));
            }
        }

        foreach (var osoba in pracownicy.Values.Where(p => !p.Dane.Kierownik))
        {
            var kierownik = kierownicy.FirstOrDefault(k => k.Dane.KodDzialu == osoba.Dane.KodDzialu);
            if (kierownik.Id != Guid.Empty && Brakuje(kierownik.Id, osoba.Id))
            {
                db.Set<SupervisorRelation>().Add(SupervisorRelation.Create(
                    tenantId, kierownik.Id, osoba.Id, DateTime.UtcNow.AddYears(-1)));
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>Grafik na bieżący i poprzedni miesiąc, dni robocze 7:00-15:00 (produkcja 6:00-14:00).</summary>
    private static async Task<int> ZapewnijGrafikiAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        Dictionary<string, (Guid Id, OsobaDemo Dane)> pracownicy,
        CancellationToken ct)
    {
        var dzis = DateOnly.FromDateTime(DateTime.UtcNow);
        var od = new DateOnly(dzis.Year, dzis.Month, 1).AddMonths(-1);
        var doDnia = new DateOnly(dzis.Year, dzis.Month, 1).AddMonths(1).AddDays(-1);

        var maJuz = await db.Set<Schedule>().IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId && s.Date >= od && s.Date <= doDnia)
            .Select(s => new { s.EmployeeId, s.Date })
            .ToListAsync(ct);
        var zbior = maJuz.Select(x => (x.EmployeeId, x.Date)).ToHashSet();

        var dodane = 0;
        foreach (var (_, (id, dane)) in pracownicy)
        {
            var start = dane.KodDzialu == "PROD" ? new TimeOnly(6, 0) : new TimeOnly(7, 0);
            var koniec = dane.KodDzialu == "PROD" ? new TimeOnly(14, 0) : new TimeOnly(15, 0);

            for (var d = od; d <= doDnia; d = d.AddDays(1))
            {
                if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                if (zbior.Contains((id, d))) continue;

                db.Set<Schedule>().Add(Schedule.Create(
                    tenantId, id, d, start, koniec, "Zmiana dzienna", null, ScheduleSource.Individual));
                dodane++;
            }
        }

        await db.SaveChangesAsync(ct);
        return dodane;
    }

    /// <summary>Zarejestrowany czas pracy za ostatnie 30 dni, z drobnymi odchyleniami od grafiku.</summary>
    private static async Task<int> ZapewnijCzasPracyAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        Dictionary<string, (Guid Id, OsobaDemo Dane)> pracownicy,
        CancellationToken ct)
    {
        var dzis = DateTime.UtcNow.Date;
        var od = dzis.AddDays(-30);

        var maJuz = await db.Set<TimeEntry>().IgnoreQueryFilters()
            .Where(e => e.TenantId == tenantId && e.EntryTime >= od)
            .Select(e => new { e.EmployeeId, e.EntryTime })
            .ToListAsync(ct);
        var zbior = maJuz.Select(x => (x.EmployeeId, x.EntryTime.Date)).ToHashSet();

        // Ziarno stałe, żeby powtórne uruchomienie dawało ten sam obraz.
        var los = new Random(20260806);
        var dodane = 0;

        foreach (var (_, (id, dane)) in pracownicy)
        {
            var bazaStart = dane.KodDzialu == "PROD" ? 6 : 7;

            for (var d = od; d < dzis; d = d.AddDays(1))
            {
                if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                if (zbior.Contains((id, d.Date))) continue;
                // Nieobecności: co jakiś czas ktoś nie przychodzi, żeby raporty nie były sterylne.
                if (los.Next(0, 100) < 6) continue;

                var wejscie = d.AddHours(bazaStart).AddMinutes(los.Next(-8, 15));
                var wyjscie = d.AddHours(bazaStart + 8).AddMinutes(los.Next(-10, 35));
                var przerwaOd = d.AddHours(bazaStart + 4);

                db.Set<TimeEntry>().AddRange(
                    TimeEntry.Create(tenantId, id, wejscie, TimeEntryType.ClockIn, ClockMethod.Qr),
                    TimeEntry.Create(tenantId, id, przerwaOd, TimeEntryType.BreakStart, ClockMethod.Kiosk),
                    TimeEntry.Create(tenantId, id, przerwaOd.AddMinutes(30), TimeEntryType.BreakEnd, ClockMethod.Kiosk),
                    TimeEntry.Create(tenantId, id, wyjscie, TimeEntryType.ClockOut, ClockMethod.Qr));
                dodane += 4;
            }
        }

        await db.SaveChangesAsync(ct);
        return dodane;
    }

    private static string Adres(OsobaDemo o)
    {
        static string Bez(string s) => new string(
            s.ToLowerInvariant()
                .Replace('ł', 'l').Replace('ą', 'a').Replace('ć', 'c').Replace('ę', 'e')
                .Replace('ń', 'n').Replace('ó', 'o').Replace('ś', 's').Replace('ź', 'z').Replace('ż', 'z')
                .Where(char.IsLetter).ToArray());

        return $"{Bez(o.Imie)}.{Bez(o.Nazwisko)}@{DomenaDemo}";
    }
}
