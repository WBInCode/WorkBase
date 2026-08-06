using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

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

        // Slowniki: provisioning najemcy ich NIE tworzy (seedery slownikowe zasilaja
        // wylacznie najemce domyslnego), wiec bez tego moduly Urlopy i Zadania sa nieuzywalne.
        var typyUrlopu = await ZapewnijTypyUrlopuAsync(db, tenantId, ct);
        var (statusy, priorytety) = await ZapewnijSlownikZadanAsync(db, tenantId, ct);

        var urlopow = await ZapewnijUrlopyAsync(db, tenantId, pracownicy, typyUrlopu, ct);
        var zadan = await ZapewnijZadaniaAsync(db, tenantId, pracownicy, statusy, priorytety, ct);
        await ZapewnijUstawieniaAsync(db, tenantId, ct);

        logger.LogInformation(
            "Gotowe: {Jednostki} jednostek, {Stanowiska} stanowisk, {Pracownicy} pracowników, {Grafiki} dni grafiku, {Wejscia} wpisów czasu pracy, {Urlopy} wniosków urlopowych, {Zadania} zadań",
            jednostki.Count, stanowiska.Count, pracownicy.Count, grafikow, wejsc, urlopow, zadan);
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

    /// <summary>Słownik nieobecności — te same wartości co dla najemcy domyślnego.</summary>
    private static async Task<Dictionary<string, Guid>> ZapewnijTypyUrlopuAsync(
        WorkBaseDbContext db, Guid tenantId, CancellationToken ct)
    {
        var istniejace = await db.Set<LeaveType>().IgnoreQueryFilters()
            .Where(t => t.TenantId == tenantId).ToListAsync(ct);
        var mapa = istniejace.ToDictionary(t => t.Code, t => t.Id);

        (string Kod, string Nazwa, bool Platny, bool Akceptacja, int? Dni, string Opis, string Kolor, int Kolejnosc)[] typy =
        [
            ("ANNUAL", "Urlop wypoczynkowy", true, true, 26, "Roczny urlop wypoczynkowy", "#4CAF50", 1),
            ("ON_DEMAND", "Urlop na żądanie", true, false, 4, "Wliczany w pulę urlopu wypoczynkowego", "#FF9800", 2),
            ("SICK", "Zwolnienie lekarskie (L4)", true, false, null, "Bez limitu dni, wymagane zaświadczenie", "#F44336", 3),
            ("CHILDCARE", "Opieka nad dzieckiem", true, true, 2, "Opieka nad dzieckiem do lat 14 (art. 188 KP)", "#9C27B0", 4),
            ("UNPAID", "Urlop bezpłatny", false, true, null, "Urlop bezpłatny na wniosek pracownika", "#607D8B", 5),
        ];

        foreach (var t in typy)
        {
            if (mapa.ContainsKey(t.Kod)) continue;
            var typ = LeaveType.Create(tenantId, t.Kod, t.Nazwa, t.Platny, t.Akceptacja, t.Dni, t.Opis, t.Kolor, t.Kolejnosc);
            db.Set<LeaveType>().Add(typ);
            await db.SaveChangesAsync(ct);
            mapa[t.Kod] = typ.Id;
        }

        return mapa;
    }

    /// <summary>Statusy, priorytety i dozwolone przejścia statusów zadań.</summary>
    private static async Task<(Dictionary<string, Guid> Statusy, Dictionary<string, Guid> Priorytety)>
        ZapewnijSlownikZadanAsync(WorkBaseDbContext db, Guid tenantId, CancellationToken ct)
    {
        var statusy = (await db.Set<TaskStatus>().IgnoreQueryFilters()
            .Where(s => s.TenantId == tenantId).ToListAsync(ct)).ToDictionary(s => s.Code, s => s.Id);

        (string Kod, string Nazwa, bool Koncowy, bool Domyslny, string Kolor, int Kolejnosc)[] def =
        [
            ("NEW", "Nowe", false, true, "#2196F3", 1),
            ("IN_PROGRESS", "W toku", false, false, "#FF9800", 2),
            ("REVIEW", "Do akceptacji", false, false, "#9C27B0", 3),
            ("CLOSED", "Zamknięte", true, false, "#4CAF50", 4),
        ];
        foreach (var s in def)
        {
            if (statusy.ContainsKey(s.Kod)) continue;
            var status = TaskStatus.Create(tenantId, s.Kod, s.Nazwa, s.Koncowy, s.Domyslny, s.Kolor, s.Kolejnosc);
            db.Set<TaskStatus>().Add(status);
            await db.SaveChangesAsync(ct);
            statusy[s.Kod] = status.Id;
        }

        var priorytety = (await db.Set<TaskPriority>().IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId).ToListAsync(ct)).ToDictionary(p => p.Code, p => p.Id);

        (string Kod, string Nazwa, string Kolor, int Kolejnosc)[] prio =
        [
            ("LOW", "Niski", "#8BC34A", 1),
            ("NORMAL", "Normalny", "#2196F3", 2),
            ("HIGH", "Wysoki", "#FF9800", 3),
            ("CRITICAL", "Krytyczny", "#F44336", 4),
        ];
        foreach (var p in prio)
        {
            if (priorytety.ContainsKey(p.Kod)) continue;
            var priorytet = TaskPriority.Create(tenantId, p.Kod, p.Nazwa, p.Kolor, p.Kolejnosc);
            db.Set<TaskPriority>().Add(priorytet);
            await db.SaveChangesAsync(ct);
            priorytety[p.Kod] = priorytet.Id;
        }

        // Pusty słownik przejść znaczy „nie skonfigurowano" (wtedy wolno wszystko),
        // ale demo ma pokazać działającą kontrolę obiegu, więc wypełniamy go jawnie.
        var maPrzejscia = await db.Set<TaskStatusTransition>().IgnoreQueryFilters()
            .AnyAsync(t => t.TenantId == tenantId, ct);
        if (!maPrzejscia)
        {
            (string Z, string Do)[] przejscia =
            [
                ("NEW", "IN_PROGRESS"), ("NEW", "CLOSED"),
                ("IN_PROGRESS", "REVIEW"), ("IN_PROGRESS", "CLOSED"), ("IN_PROGRESS", "NEW"),
                ("REVIEW", "CLOSED"), ("REVIEW", "IN_PROGRESS"),
                ("CLOSED", "IN_PROGRESS"),
            ];
            foreach (var (z, doStatusu) in przejscia)
            {
                db.Set<TaskStatusTransition>().Add(
                    TaskStatusTransition.Create(tenantId, statusy[z], statusy[doStatusu]));
            }
            await db.SaveChangesAsync(ct);
        }

        return (statusy, priorytety);
    }

    /// <summary>Pule urlopowe na bieżący rok i wnioski w różnych stanach obiegu.</summary>
    private static async Task<int> ZapewnijUrlopyAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        Dictionary<string, (Guid Id, OsobaDemo Dane)> pracownicy,
        Dictionary<string, Guid> typy,
        CancellationToken ct)
    {
        var rok = DateTime.UtcNow.Year;

        var maBilans = (await db.Set<LeaveBalance>().IgnoreQueryFilters()
            .Where(b => b.TenantId == tenantId && b.Year == rok)
            .Select(b => new { b.EmployeeId, b.LeaveTypeId }).ToListAsync(ct))
            .Select(x => (x.EmployeeId, x.LeaveTypeId)).ToHashSet();

        var los = new Random(20260806);
        var bilanse = new Dictionary<Guid, LeaveBalance>();

        foreach (var (_, (id, _)) in pracownicy)
        {
            foreach (var kod in new[] { "ANNUAL", "ON_DEMAND", "CHILDCARE" })
            {
                if (maBilans.Contains((id, typy[kod]))) continue;
                var pula = kod switch { "ANNUAL" => 26m, "ON_DEMAND" => 4m, _ => 2m };
                var przeniesione = kod == "ANNUAL" ? los.Next(0, 6) : 0;
                var bilans = LeaveBalance.Create(tenantId, id, typy[kod], rok, pula, przeniesione);
                db.Set<LeaveBalance>().Add(bilans);
                if (kod == "ANNUAL") bilanse[id] = bilans;
            }
        }
        await db.SaveChangesAsync(ct);

        var maWnioski = await db.Set<LeaveRequest>().IgnoreQueryFilters()
            .AnyAsync(r => r.TenantId == tenantId, ct);
        if (maWnioski) return 0;

        var dzis = DateTime.UtcNow.Date;
        var dodane = 0;

        foreach (var (_, (id, dane)) in pracownicy)
        {
            // Urlop zakonczony w zeszlym miesiacu.
            var odPrzeszly = dzis.AddDays(-los.Next(25, 50));
            var doPrzeszly = odPrzeszly.AddDays(los.Next(2, 6));
            var dniPrzeszly = LiczDniRobocze(odPrzeszly, doPrzeszly);
            var wniosek = LeaveRequest.Create(tenantId, id, typy["ANNUAL"],
                odPrzeszly, doPrzeszly, dniPrzeszly, "Wypoczynek");
            wniosek.Submit();
            wniosek.Approve();
            db.Set<LeaveRequest>().Add(wniosek);
            if (bilanse.TryGetValue(id, out var b1)) { b1.AddPending(dniPrzeszly); b1.ConfirmUsed(dniPrzeszly); }
            dodane++;

            // Co trzecia osoba ma wniosek czekajacy na decyzje przelozonego.
            if (los.Next(0, 3) == 0)
            {
                var odPrzyszly = dzis.AddDays(los.Next(7, 40));
                var doPrzyszly = odPrzyszly.AddDays(los.Next(3, 10));
                var dniPrzyszly = LiczDniRobocze(odPrzyszly, doPrzyszly);
                var oczekujacy = LeaveRequest.Create(tenantId, id, typy["ANNUAL"],
                    odPrzyszly, doPrzyszly, dniPrzyszly, "Urlop letni");
                oczekujacy.Submit();
                db.Set<LeaveRequest>().Add(oczekujacy);
                if (bilanse.TryGetValue(id, out var b2)) b2.AddPending(dniPrzyszly);
                dodane++;
            }

            // Pojedyncze zwolnienia lekarskie, zeby kalendarz nieobecnosci nie byl jednorodny.
            if (los.Next(0, 5) == 0)
            {
                var odL4 = dzis.AddDays(-los.Next(3, 20));
                var l4 = LeaveRequest.Create(tenantId, id, typy["SICK"],
                    odL4, odL4.AddDays(los.Next(1, 4)), los.Next(1, 4), "Zwolnienie lekarskie");
                l4.Submit();
                l4.Approve();
                db.Set<LeaveRequest>().Add(l4);
                dodane++;
            }

            _ = dane;
        }

        await db.SaveChangesAsync(ct);
        return dodane;
    }

    private static decimal LiczDniRobocze(DateTime od, DateTime doDnia)
    {
        var dni = 0;
        for (var d = od; d <= doDnia; d = d.AddDays(1))
        {
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) dni++;
        }
        return dni;
    }

    /// <summary>Zadania rozłożone po działach, z komentarzami i różnymi statusami.</summary>
    private static async Task<int> ZapewnijZadaniaAsync(
        WorkBaseDbContext db,
        Guid tenantId,
        Dictionary<string, (Guid Id, OsobaDemo Dane)> pracownicy,
        Dictionary<string, Guid> statusy,
        Dictionary<string, Guid> priorytety,
        CancellationToken ct)
    {
        if (await db.Set<TaskItem>().IgnoreQueryFilters().AnyAsync(t => t.TenantId == tenantId, ct))
            return 0;

        (string Dzial, string Tytul, string Opis)[] szablony =
        [
            ("PROD", "Przegląd okresowy tokarki CNC-3", "Przegląd zgodnie z kartą serwisową, wymiana filtrów i oleju."),
            ("PROD", "Wdrożenie nowej karty technologicznej", "Aktualizacja parametrów obróbki dla serii 400-Z."),
            ("PROD", "Analiza braków z partii 2026/07", "Ustalić przyczynę odchyłek wymiarowych i opisać działania naprawcze."),
            ("PROD", "Szkolenie BHP przy obrabiarkach", "Powtórka instruktażu stanowiskowego dla operatorów."),
            ("SPRZ", "Przygotować ofertę dla Metalpol sp. z o.o.", "Zapytanie na 1200 szt. korpusów, termin odpowiedzi 5 dni."),
            ("SPRZ", "Kontakt z klientem po wysyłce próbek", "Zebrać opinię i ustalić dalsze kroki."),
            ("SPRZ", "Aktualizacja cennika na III kwartał", "Uwzględnić wzrost cen materiału i kosztów energii."),
            ("SPRZ", "Podsumowanie sprzedaży za lipiec", "Zestawienie realizacji planu w podziale na handlowców."),
            ("MAG", "Inwentaryzacja strefy A", "Spis z natury regałów A1-A12, zgłosić rozbieżności."),
            ("MAG", "Reklamacja dostawy od Stalexport", "Braki ilościowe w dostawie 2026/1187."),
            ("MAG", "Przegląd wózków widłowych", "Terminy UDT i stan techniczny."),
            ("IT", "Wymiana stacji roboczych w księgowości", "4 komputery, migracja danych i konfiguracja drukarek."),
            ("IT", "Wdrożenie kopii zapasowych offsite", "Kopie poza siedzibą, test odtworzenia raz w miesiącu."),
            ("IT", "Przegląd uprawnień w systemach", "Weryfikacja dostępów po zmianach kadrowych."),
            ("KSIE", "Zamknięcie miesiąca lipiec", "Uzgodnienie kont, rozliczenie międzyokresowe."),
            ("KSIE", "Rozliczenie delegacji handlowców", "Zebrać dokumenty i rozliczyć zaliczki."),
            ("KSIE", "Przygotowanie danych do JPK", "Weryfikacja rejestrów VAT za lipiec."),
            ("HR", "Nabór na stanowisko operatora CNC", "Publikacja ogłoszenia i wstępna selekcja kandydatów."),
            ("HR", "Przegląd terminów badań okresowych", "Lista osób z badaniami wygasającymi w tym kwartale."),
            ("HR", "Ocena okresowa pracowników produkcji", "Rozmowy podsumowujące pierwsze półrocze."),
            ("ZARZ", "Plan inwestycji na 2027", "Zebrać potrzeby działów i oszacować budżet."),
            ("ZARZ", "Przegląd wyników kwartalnych", "Analiza marży i realizacji planu sprzedaży."),
        ];

        var los = new Random(20260806);
        var dzis = DateTime.UtcNow.Date;
        var poDzialach = pracownicy.Values.GroupBy(p => p.Dane.KodDzialu)
            .ToDictionary(g => g.Key, g => g.ToList());
        var dodane = 0;

        foreach (var szablon in szablony)
        {
            if (!poDzialach.TryGetValue(szablon.Dzial, out var zespol) || zespol.Count == 0) continue;

            var kierownik = zespol.FirstOrDefault(p => p.Dane.Kierownik);
            var wykonawca = zespol[los.Next(zespol.Count)];
            var kodStatusu = los.Next(0, 10) switch
            {
                < 3 => "NEW",
                < 6 => "IN_PROGRESS",
                < 8 => "REVIEW",
                _ => "CLOSED",
            };
            var kodPriorytetu = los.Next(0, 10) switch
            {
                < 2 => "LOW",
                < 7 => "NORMAL",
                < 9 => "HIGH",
                _ => "CRITICAL",
            };

            var zadanie = TaskItem.Create(
                tenantId, szablon.Tytul, statusy[kodStatusu], priorytety[kodPriorytetu],
                wykonawca.Id, kierownik.Id == Guid.Empty ? null : kierownik.Id,
                szablon.Opis, dzis.AddDays(los.Next(-10, 25)));
            db.Set<TaskItem>().Add(zadanie);
            await db.SaveChangesAsync(ct);
            dodane++;

            if (kodStatusu != "NEW" && kierownik.Id != Guid.Empty)
            {
                db.Set<TaskComment>().Add(TaskComment.Create(
                    tenantId, zadanie.Id, kierownik.Id,
                    kodStatusu == "CLOSED"
                        ? "Zamykam, wynik zgodny z ustaleniami."
                        : "Proszę o informację o postępach do końca tygodnia."));
                await db.SaveChangesAsync(ct);
            }
        }

        return dodane;
    }

    /// <summary>Ustawienia modułów, żeby panel konfiguracji nie był pusty.</summary>
    private static async Task ZapewnijUstawieniaAsync(
        WorkBaseDbContext db, Guid tenantId, CancellationToken ct)
    {
        var istniejace = await db.Set<TenantConfig>()
            .Where(c => c.TenantId == tenantId).Select(c => c.Key).ToListAsync(ct);

        (string Klucz, string Wartosc)[] ustawienia =
        [
            ("document_upload", JsonSerializer.Serialize(new
            {
                MaxFileSizeBytes = 25L * 1024 * 1024,
                AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".zip" },
            })),
            ("task_overdue", JsonSerializer.Serialize(new { GracePeriodHours = 24, NotifyOnOverdue = true })),
            ("anomaly_detection", JsonSerializer.Serialize(new
            {
                LateArrivalThreshold = "00:10:00",
                ExcessiveShiftThreshold = "11:00:00",
                DetectMissingClockOut = true,
                DetectLateArrival = true,
                DetectDoubleClockIn = true,
                DetectExcessiveShift = true,
                DetectMissingClockIn = true,
                DetectWorkOnDayOff = true,
            })),
            ("payroll.overtime_multiplier", "1.5"),
            ("payroll.night_multiplier", "1.2"),
            ("payroll.holiday_multiplier", "2.0"),
        ];

        foreach (var (klucz, wartosc) in ustawienia)
        {
            if (istniejace.Contains(klucz)) continue;
            db.Set<TenantConfig>().Add(new TenantConfig
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                Key = klucz,
                Value = wartosc,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
