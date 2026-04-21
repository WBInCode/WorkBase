using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Forms.Application.Contracts;
using WorkBase.Modules.Forms.Domain.Entities;

namespace WorkBase.Modules.Forms.Infrastructure.Repositories;

public sealed class FormDefinitionRepository(WorkBaseDbContext db) : IFormDefinitionRepository
{
    public async Task<FormDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<FormDefinition>().FindAsync([id], ct);

    public async Task<FormDefinition?> GetByIdWithFieldsAsync(Guid id, CancellationToken ct = default)
        => await db.Set<FormDefinition>().Include(d => d.Fields).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<List<FormDefinition>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
        => await db.Set<FormDefinition>().Where(d => d.TenantId == tenantId).OrderBy(d => d.Name).ToListAsync(ct);

    public async Task AddAsync(FormDefinition definition, CancellationToken ct = default)
        => await db.Set<FormDefinition>().AddAsync(definition, ct);

    public void Update(FormDefinition definition) => db.Set<FormDefinition>().Update(definition);
    public void Remove(FormDefinition definition) => db.Set<FormDefinition>().Remove(definition);
}

public sealed class FormFieldRepository(WorkBaseDbContext db) : IFormFieldRepository
{
    public async Task<List<FormField>> GetByDefinitionAsync(Guid formDefinitionId, CancellationToken ct = default)
        => await db.Set<FormField>().Where(f => f.FormDefinitionId == formDefinitionId).OrderBy(f => f.Order).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<FormField> fields, CancellationToken ct = default)
        => await db.Set<FormField>().AddRangeAsync(fields, ct);

    public void RemoveRange(IEnumerable<FormField> fields) => db.Set<FormField>().RemoveRange(fields);
}

public sealed class FormSubmissionRepository(WorkBaseDbContext db) : IFormSubmissionRepository
{
    public async Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<FormSubmission>().FindAsync([id], ct);

    public async Task<List<FormSubmission>> GetByDefinitionAsync(Guid formDefinitionId, CancellationToken ct = default)
        => await db.Set<FormSubmission>().Where(s => s.FormDefinitionId == formDefinitionId).OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public async Task<List<FormSubmission>> GetBySubmitterAsync(Guid submittedBy, CancellationToken ct = default)
        => await db.Set<FormSubmission>().Where(s => s.SubmittedBy == submittedBy).OrderByDescending(s => s.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(FormSubmission submission, CancellationToken ct = default)
        => await db.Set<FormSubmission>().AddAsync(submission, ct);

    public void Update(FormSubmission submission) => db.Set<FormSubmission>().Update(submission);
}
