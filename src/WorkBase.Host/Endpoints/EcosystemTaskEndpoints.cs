using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Entities;
using TaskStatusEntity = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Host.Endpoints;

/// <summary>
/// Udostępnienie zadań innym aplikacjom ekosystemu (czat wstawia wzmianki).
/// Domyślnie wyłączone — bez sekretu endpoint nie istnieje.
/// </summary>
public sealed class TaskSearchOptions
{
    public const string SectionName = "TaskSearch";

    public bool Enabled { get; init; }

    /// <summary>Sekret współdzielony, oczekiwany w nagłówku <c>x-wb-task-secret</c>.</summary>
    public string Secret { get; init; } = "";
}

public static class EcosystemTaskEndpoints
{
    public static IEndpointRouteBuilder MapEcosystemTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/ecosystem/tasks", async (
            HttpContext http,
            WorkBaseDbContext db,
            IConfiguration configuration,
            string? email,
            string? q,
            int? limit,
            CancellationToken ct) =>
        {
            var options = configuration.GetSection(TaskSearchOptions.SectionName).Get<TaskSearchOptions>()
                ?? new TaskSearchOptions();
            if (!options.Enabled || string.IsNullOrWhiteSpace(options.Secret))
                return Results.NotFound();

            if (!SekretPasuje(options.Secret, http.Request.Headers["x-wb-task-secret"].ToString()))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(email))
                return Results.BadRequest(new { error = "EMAIL_REQUIRED" });

            var ile = Math.Clamp(limit ?? 10, 1, 25);
            var adres = email.Trim().ToLowerInvariant();

            // Zapytania spoza żądania użytkownika nie mają kontekstu najemcy,
            // więc filtry globalne trzeba pominąć i zawęzić zakres ręcznie.
            var pracownik = await db.Set<Employee>()
                .IgnoreQueryFilters()
                .Where(item => item.Email != null && item.Email.ToLower() == adres)
                .Select(item => new { item.Id, item.TenantId })
                .FirstOrDefaultAsync(ct);

            // Brak pracownika zwracamy jako pusty wynik, a nie 404: inaczej pytająca
            // aplikacja mogłaby sprawdzać, kto pracuje w tej organizacji.
            if (pracownik is null)
                return Results.Ok(new { tasks = Array.Empty<object>() });

            var fraza = (q ?? "").Trim().ToLowerInvariant();

            // Wyłącznie zadania tej osoby — wyszukiwarka w innej aplikacji nie może
            // pokazywać cudzej pracy, bo tamta strona nie zna uprawnień WorkBase.
            var zapytanie =
                from zadanie in db.Set<TaskItem>().IgnoreQueryFilters()
                join status in db.Set<TaskStatusEntity>().IgnoreQueryFilters()
                    on zadanie.StatusId equals status.Id
                where zadanie.TenantId == pracownik.TenantId
                    && (zadanie.AssigneeId == pracownik.Id || zadanie.ReporterId == pracownik.Id)
                    && !status.IsFinal
                    && (fraza == "" || zadanie.Title.ToLower().Contains(fraza))
                orderby zadanie.DueDate, zadanie.CreatedAt descending
                select new { id = zadanie.Id, title = zadanie.Title, status = status.Name };

            var zadania = await zapytanie.Take(ile).ToListAsync(ct);
            return Results.Ok(new { tasks = zadania });
        })
        .AllowAnonymous()
        .WithName("EcosystemTaskSearch")
        .WithSummary("Wyszukiwarka zadań dla aplikacji ekosystemu");

        return endpoints;
    }

    private static bool SekretPasuje(string oczekiwany, string podany)
    {
        var a = Encoding.UTF8.GetBytes(oczekiwany);
        var b = Encoding.UTF8.GetBytes(podany);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
