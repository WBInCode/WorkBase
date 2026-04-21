using WorkBase.Modules.Forms.Domain.Entities;

namespace WorkBase.Modules.Forms.Application.Contracts;

public interface IFormDefinitionRepository
{
    Task<FormDefinition?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FormDefinition?> GetByIdWithFieldsAsync(Guid id, CancellationToken ct = default);
    Task<List<FormDefinition>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(FormDefinition definition, CancellationToken ct = default);
    void Update(FormDefinition definition);
    void Remove(FormDefinition definition);
}

public interface IFormFieldRepository
{
    Task<List<FormField>> GetByDefinitionAsync(Guid formDefinitionId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<FormField> fields, CancellationToken ct = default);
    void RemoveRange(IEnumerable<FormField> fields);
}

public interface IFormSubmissionRepository
{
    Task<FormSubmission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<FormSubmission>> GetByDefinitionAsync(Guid formDefinitionId, CancellationToken ct = default);
    Task<List<FormSubmission>> GetBySubmitterAsync(Guid submittedBy, CancellationToken ct = default);
    Task AddAsync(FormSubmission submission, CancellationToken ct = default);
    void Update(FormSubmission submission);
}
