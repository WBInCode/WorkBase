using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record UpdateTimeCorrectionCommand(
    Guid Id, DateTime CorrectedClockIn, DateTime CorrectedClockOut,
    string Reason) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateTimeCorrectionHandler(ITimeCorrectionRepository repository)
    : ICommandHandler<UpdateTimeCorrectionCommand>
{
    public async Task<Result> Handle(UpdateTimeCorrectionCommand request, CancellationToken cancellationToken)
    {
        var correction = await repository.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
        if (correction is null)
            return Result.Failure(Error.NotFound("TimeCorrection.NotFound",
                $"Korekta o id '{request.Id}' nie została znaleziona."));

        correction.Update(request.CorrectedClockIn, request.CorrectedClockOut, request.Reason);
        return Result.Success();
    }
}
