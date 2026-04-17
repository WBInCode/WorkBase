using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record CreateTimeCorrectionCommand(
    Guid EmployeeId,
    DateOnly Date,
    DateTime OriginalClockIn,
    DateTime OriginalClockOut,
    DateTime CorrectedClockIn,
    DateTime CorrectedClockOut,
    string Reason,
    string CorrectedBy,
    Guid? TimeSheetId = null) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateTimeCorrectionHandler(ITimeCorrectionRepository repository)
    : ICommandHandler<CreateTimeCorrectionCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTimeCorrectionCommand request, CancellationToken cancellationToken)
    {
        var correction = TimeCorrection.Create(
            request.TenantId,
            request.EmployeeId,
            request.Date,
            request.OriginalClockIn,
            request.OriginalClockOut,
            request.CorrectedClockIn,
            request.CorrectedClockOut,
            request.Reason,
            request.CorrectedBy,
            request.TimeSheetId);

        await repository.AddAsync(correction, cancellationToken);
        return correction.Id;
    }
}
