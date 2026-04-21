using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;

namespace WorkBase.Host.Endpoints;

public static class DepartmentModuleEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/config/departments").WithTags("DepartmentModules").RequireAuthorization();

        group.MapGet("/", async (WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var modules = await db.Set<DepartmentModule>()
                .Where(m => m.TenantId == tenantId.Value && m.IsActive)
                .OrderBy(m => m.Name).ToListAsync();
            return Results.Ok(modules);
        }).WithName("GetDepartmentModules").WithSummary("Pobierz moduły działowe");

        group.MapGet("/{id:guid}", async (Guid id, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var mod = await db.Set<DepartmentModule>().FirstOrDefaultAsync(m => m.Id == id && m.TenantId == tenantId.Value);
            if (mod is null) return Results.NotFound();

            var forms = await db.Set<DepartmentModuleForm>().Where(f => f.DepartmentModuleId == id).OrderBy(f => f.SortOrder).ToListAsync();
            var workflows = await db.Set<DepartmentModuleWorkflow>().Where(w => w.DepartmentModuleId == id).OrderBy(w => w.SortOrder).ToListAsync();
            return Results.Ok(new { Module = mod, Forms = forms, Workflows = workflows });
        }).WithName("GetDepartmentModule").WithSummary("Pobierz moduł działowy z formularzami i workflow");

        group.MapPost("/", async (CreateDepartmentModuleRequest req, WorkBaseDbContext db, HttpContext http) =>
        {
            var tenantId = http.User.GetTenantId();
            if (tenantId is null) return Results.Forbid();
            var mod = new DepartmentModule
            {
                Id = Guid.NewGuid(), TenantId = tenantId.Value, OrgUnitId = req.OrgUnitId,
                ModuleType = req.ModuleType, Name = req.Name, Description = req.Description,
                Icon = req.Icon, ConfigJson = req.ConfigJson ?? "{}", CreatedAt = DateTime.UtcNow
            };
            db.Set<DepartmentModule>().Add(mod);
            await db.SaveChangesAsync();
            return Results.Created($"/api/config/departments/{mod.Id}", mod);
        }).WithName("CreateDepartmentModule").RequirePermission("config.manage");

        group.MapPost("/{id:guid}/forms", async (Guid id, LinkFormRequest req, WorkBaseDbContext db) =>
        {
            var link = new DepartmentModuleForm
            {
                Id = Guid.NewGuid(), DepartmentModuleId = id,
                FormDefinitionId = req.FormDefinitionId, SortOrder = req.SortOrder, Label = req.Label
            };
            db.Set<DepartmentModuleForm>().Add(link);
            await db.SaveChangesAsync();
            return Results.Created($"/api/config/departments/{id}/forms/{link.Id}", link);
        }).WithName("LinkFormToDepartment").RequirePermission("config.manage");

        group.MapPost("/{id:guid}/workflows", async (Guid id, LinkWorkflowRequest req, WorkBaseDbContext db) =>
        {
            var link = new DepartmentModuleWorkflow
            {
                Id = Guid.NewGuid(), DepartmentModuleId = id,
                WorkflowDefinitionId = req.WorkflowDefinitionId, SortOrder = req.SortOrder, Label = req.Label
            };
            db.Set<DepartmentModuleWorkflow>().Add(link);
            await db.SaveChangesAsync();
            return Results.Created($"/api/config/departments/{id}/workflows/{link.Id}", link);
        }).WithName("LinkWorkflowToDepartment").RequirePermission("config.manage");

        return endpoints;
    }
}

public sealed record CreateDepartmentModuleRequest(Guid OrgUnitId, DepartmentModuleType ModuleType, string Name, string? Description, string? Icon, string? ConfigJson);
public sealed record LinkFormRequest(Guid FormDefinitionId, int SortOrder, string? Label);
public sealed record LinkWorkflowRequest(Guid WorkflowDefinitionId, int SortOrder, string? Label);
