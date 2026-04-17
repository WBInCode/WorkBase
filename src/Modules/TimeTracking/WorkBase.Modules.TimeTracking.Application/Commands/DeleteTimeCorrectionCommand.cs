using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed record DeleteTimeCorrectionCommand(Guid Id) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteTimeCorrectionHandler(ITimeCorrectionRepository repository)
    : ICommandHandler<DeleteTimeCorrectionCommand>
{
    public async Task<Result> Handle(DeleteTimeCorrectionCommand request, CancellationToken cancellationToken)
    {
        var correction = await repository.GetByIdAsync(request.TenantId, request.Id, cancellationToken);
        if (correction is null)
            return Result.Failure(Error.NotFound("TimeCorrection.NotFound",
                $"Korekta o id '{request.Id}' nie została znaleziona."));

        repository.Remove(correction);
        return Result.Success();
    }
}
