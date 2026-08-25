using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;

namespace WorkBase.Modules.Dashboard.Infrastructure.Queries;

/// <summary>
/// Liczy pozycje wymagające uwagi kierownika.
/// </summary>
/// <remarks>
/// Pulpit pokazywał dotąd same liczby: obecni, spóźnieni, otwarte zadania. Kierownik rzadko
/// potrzebuje liczby — potrzebuje listy rzeczy do zrobienia dziś rano. Wszystkie poniższe
/// wynikają z danych, które system już ma; brakowało wyłącznie zapytań.
///
/// Zakres pracowników przychodzi z zewnątrz jako lista identyfikatorów i jest rozstrzygany
/// tym samym mechanizmem co przy stawkach godzinowych — zapytanie samo niczego nie zawęża.
/// </remarks>
public sealed class DapperAlertyQueryService(IConfiguration configuration) : IAlertyQueryService
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    /// <summary>Ile pozycji pokazujemy z nazwiska, zanim reszta zostanie samą liczbą.</summary>
    private const int MaksPozycji = 5;

    public async Task<List<AlertDto>> PobierzAsync(
        Guid tenantId,
        IReadOnlyList<Guid> pracownicyWZakresie,
        Guid? akceptantEmployeeId,
        int dniOczekiwaniaNaDecyzje,
        bool pokazujStawki,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var alerty = new List<AlertDto>();

        if (akceptantEmployeeId is Guid akceptant)
        {
            var czekajace = await ZalegleWnioskiAsync(
                connection, tenantId, akceptant, dniOczekiwaniaNaDecyzje);

            if (czekajace.Count > 0)
            {
                alerty.Add(new AlertDto(
                    "wnioski-do-decyzji", "pilne",
                    "Wnioski czekają na Twoją decyzję",
                    $"Ktoś czeka dłużej niż {dniOczekiwaniaNaDecyzje} dni.",
                    czekajace.Count, "/leave/approvals", Skroc(czekajace)));
            }
        }

        if (pracownicyWZakresie.Count == 0) return alerty;

        var pracownicy = pracownicyWZakresie.ToArray();

        var nieobecni = await NieobecniMimoGrafikuAsync(connection, tenantId, pracownicy);
        if (nieobecni.Count > 0)
        {
            alerty.Add(new AlertDto(
                "nieobecni-dzis", "pilne",
                "Nie zarejestrowali wejścia",
                "Mają dziś grafik, ale nie rozpoczęli pracy.",
                nieobecni.Count, "/time/team-report", Skroc(nieobecni)));
        }

        var bezPrzelozonego = await BezPrzelozonegoAsync(connection, tenantId, pracownicy);
        if (bezPrzelozonego.Count > 0)
        {
            alerty.Add(new AlertDto(
                "bez-przelozonego", "pilne",
                "Pracownicy bez przełożonego",
                "Ich wnioski nie mają komu trafić do akceptacji.",
                bezPrzelozonego.Count, "/org/employees", Skroc(bezPrzelozonego)));
        }

        if (pokazujStawki)
        {
            var bezStawki = await BezStawkiAsync(connection, tenantId, pracownicy);
            if (bezStawki.Count > 0)
            {
                alerty.Add(new AlertDto(
                    "bez-stawki", "uwaga",
                    "Pracownicy bez stawki godzinowej",
                    "Bez stawki nie policzymy im wynagrodzenia.",
                    bezStawki.Count, "/payroll", Skroc(bezStawki)));
            }
        }

        var anomalie = await NierozpatrzoneAnomalieAsync(connection, tenantId, pracownicy);
        if (anomalie.Count > 0)
        {
            alerty.Add(new AlertDto(
                "anomalie", "uwaga",
                "Nierozpatrzone anomalie czasu pracy",
                "Zgłoszenia czekają na sprawdzenie.",
                anomalie.Count, "/time/team-report", Skroc(anomalie)));
        }

        return alerty;
    }

    private static IReadOnlyList<PozycjaAlertuDto> Skroc(List<PozycjaAlertuDto> pozycje)
        => pozycje.Count <= MaksPozycji ? pozycje : pozycje.Take(MaksPozycji).ToList();

    private static async Task<List<PozycjaAlertuDto>> ZalegleWnioskiAsync(
        NpgsqlConnection connection, Guid tenantId, Guid akceptant, int dni)
    {
        // requester_id wskazuje pracownika skladajacego wniosek — bez zlaczenia po nim
        // kierownik zobaczylby same identyfikatory.
        const string sql = """
            SELECT ar.id AS Id,
                   COALESCE(e.first_name || ' ' || e.last_name, 'Wniosek') AS Opis
            FROM wf_approval_requests ar
            LEFT JOIN org_employees e
                   ON e.id = ar.requester_id AND e.tenant_id = ar.tenant_id
            WHERE ar.tenant_id = @TenantId
              AND ar.approver_id = @Akceptant
              AND ar.status = 'Pending'
              AND ar.created_at <= @Granica
            ORDER BY ar.created_at
            """;

        var granica = DateTime.UtcNow.AddDays(-dni);
        return (await connection.QueryAsync<PozycjaAlertuDto>(
            sql, new { TenantId = tenantId, Akceptant = akceptant, Granica = granica })).ToList();
    }

    private static async Task<List<PozycjaAlertuDto>> NieobecniMimoGrafikuAsync(
        NpgsqlConnection connection, Guid tenantId, Guid[] pracownicy)
    {
        const string sql = """
            SELECT DISTINCT e.id AS Id, e.first_name || ' ' || e.last_name AS Opis
            FROM time_schedules s
            INNER JOIN org_employees e ON e.id = s.employee_id AND e.tenant_id = s.tenant_id
            WHERE s.tenant_id = @TenantId
              AND s.employee_id = ANY(@Pracownicy)
              AND s.date = @Dzis::date
              AND NOT EXISTS (
                  SELECT 1 FROM time_entries te
                  WHERE te.tenant_id = s.tenant_id
                    AND te.employee_id = s.employee_id
                    AND te.type = 'ClockIn'
                    AND te.entry_time >= @Poczatek AND te.entry_time < @Koniec
              )
            ORDER BY 2
            """;

        var dzis = DateTime.UtcNow.Date;
        return (await connection.QueryAsync<PozycjaAlertuDto>(sql, new
        {
            TenantId = tenantId,
            Pracownicy = pracownicy,
            Dzis = dzis,
            Poczatek = dzis,
            Koniec = dzis.AddDays(1),
        })).ToList();
    }

    private static async Task<List<PozycjaAlertuDto>> BezPrzelozonegoAsync(
        NpgsqlConnection connection, Guid tenantId, Guid[] pracownicy)
    {
        const string sql = """
            SELECT e.id AS Id, e.first_name || ' ' || e.last_name AS Opis
            FROM org_employees e
            WHERE e.tenant_id = @TenantId
              AND e.id = ANY(@Pracownicy)
              AND e.status = 'Active'
              AND NOT EXISTS (
                  SELECT 1 FROM org_supervisor_relations r
                  WHERE r.tenant_id = e.tenant_id
                    AND r.subordinate_employee_id = e.id
                    AND r.end_date IS NULL
              )
            ORDER BY 2
            """;

        return (await connection.QueryAsync<PozycjaAlertuDto>(
            sql, new { TenantId = tenantId, Pracownicy = pracownicy })).ToList();
    }

    private static async Task<List<PozycjaAlertuDto>> BezStawkiAsync(
        NpgsqlConnection connection, Guid tenantId, Guid[] pracownicy)
    {
        const string sql = """
            SELECT e.id AS Id, e.first_name || ' ' || e.last_name AS Opis
            FROM org_employees e
            WHERE e.tenant_id = @TenantId
              AND e.id = ANY(@Pracownicy)
              AND e.status = 'Active'
              AND e.hourly_rate IS NULL
            ORDER BY 2
            """;

        return (await connection.QueryAsync<PozycjaAlertuDto>(
            sql, new { TenantId = tenantId, Pracownicy = pracownicy })).ToList();
    }

    private static async Task<List<PozycjaAlertuDto>> NierozpatrzoneAnomalieAsync(
        NpgsqlConnection connection, Guid tenantId, Guid[] pracownicy)
    {
        const string sql = """
            SELECT a.id AS Id,
                   e.first_name || ' ' || e.last_name || ' — ' || to_char(a.date, 'DD.MM') AS Opis
            FROM time_anomalies a
            INNER JOIN org_employees e ON e.id = a.employee_id AND e.tenant_id = a.tenant_id
            WHERE a.tenant_id = @TenantId
              AND a.employee_id = ANY(@Pracownicy)
              AND a.status = 'New'
            ORDER BY a.date DESC
            """;

        return (await connection.QueryAsync<PozycjaAlertuDto>(
            sql, new { TenantId = tenantId, Pracownicy = pracownicy })).ToList();
    }
}
