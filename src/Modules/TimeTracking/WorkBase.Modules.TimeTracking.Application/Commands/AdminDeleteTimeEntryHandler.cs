using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class AdminDeleteTimeEntryHandler(ITimeEntryRepository timeEntryRepository)
    : ICommandHandler<AdminDeleteTimeEntryCommand>
{
    public async Task<Result> Handle(AdminDeleteTimeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await timeEntryRepository.GetByIdAsync(request.TenantId, request.EntryId, cancellationToken);
        if (entry is null)
            return Result.Failure(Error.NotFound("TimeEntry.NotFound", "Wpis nie został znaleziony."));

        timeEntryRepository.Delete(entry);

        return Result.Success();
    }
}
