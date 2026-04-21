using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

public static class CardSectionEndpoints
{
    public static IEndpointRouteBuilder MapCardSectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/config/card-sections")
            .WithTags("CardSections")
            .RequireAuthorization();

        group.MapGet("/{entityType}", async (string entityType, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var sections = await db.Set<CardSection>()
                .Where(s => s.TenantId == tenantId.Value && s.EntityType == entityType)
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
            return Results.Ok(sections);
        })
        .WithName("GetCardSections")
        .WithSummary("Pobierz sekcje karty dla typu encji");

        group.MapPost("/", async (CreateCardSectionRequest request, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var section = new CardSection
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId.Value,
                EntityType = request.EntityType,
                SectionName = request.SectionName,
                Icon = request.Icon,
                SortOrder = request.SortOrder,
                IsCollapsedByDefault = request.IsCollapsedByDefault,
                CreatedAt = DateTime.UtcNow
            };
            db.Set<CardSection>().Add(section);
            await db.SaveChangesAsync();
            return Results.Created($"/api/config/card-sections/{section.Id}", section);
        })
        .WithName("CreateCardSection")
        .WithSummary("Utwórz sekcję karty")
        .RequirePermission("config.manage");

        group.MapPut("/{id:guid}", async (Guid id, UpdateCardSectionRequest request, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var section = await db.Set<CardSection>().FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value);
            if (section is null) return Results.NotFound();

            section.SectionName = request.SectionName;
            section.Icon = request.Icon;
            section.SortOrder = request.SortOrder;
            section.IsCollapsedByDefault = request.IsCollapsedByDefault;
            await db.SaveChangesAsync();
            return Results.Ok(section);
        })
        .WithName("UpdateCardSection")
        .WithSummary("Aktualizuj sekcję karty")
        .RequirePermission("config.manage");

        group.MapDelete("/{id:guid}", async (Guid id, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();

            var section = await db.Set<CardSection>().FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId.Value);
            if (section is null) return Results.NotFound();

            db.Set<CardSection>().Remove(section);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteCardSection")
        .WithSummary("Usuń sekcję karty")
        .RequirePermission("config.manage");

        return endpoints;
    }
}

public sealed record CreateCardSectionRequest(string EntityType, string SectionName, string? Icon, int SortOrder, bool IsCollapsedByDefault);
public sealed record UpdateCardSectionRequest(string SectionName, string? Icon, int SortOrder, bool IsCollapsedByDefault);
