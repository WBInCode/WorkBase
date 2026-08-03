using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Application.Services;

public interface ITaskStatusMachine
{
    Task<Result> ValidateTransitionAsync(Guid tenantId, Guid fromStatusId, Guid toStatusId, CancellationToken cancellationToken = default);
}

public sealed class TaskStatusMachine(
    ITaskStatusTransitionRepository transitionRepository,
    ITaskStatusRepository statusRepository) : ITaskStatusMachine
{
    public async Task<Result> ValidateTransitionAsync(
        Guid tenantId, Guid fromStatusId, Guid toStatusId,
        CancellationToken cancellationToken = default)
    {
        if (fromStatusId == toStatusId)
            return Result.Failure(new Error("Task.SameStatus",
                "Zadanie jest już w tym statusie."));

        var toStatus = await statusRepository.GetByIdAsync(toStatusId, cancellationToken);
        if (toStatus is null)
            return Result.Failure(Error.NotFound("Task.StatusNotFound",
                $"Status o id '{toStatusId}' nie został znaleziony."));

        if (!toStatus.IsActive)
            return Result.Failure(new Error("Task.StatusInactive",
                $"Status '{toStatus.Name}' jest nieaktywny."));

        var isAllowed = await transitionRepository.IsTransitionAllowedAsync(
            tenantId, fromStatusId, toStatusId, cancellationToken);

        if (isAllowed)
            return Result.Success();

        // Slownik przejsc nie ma zadnego interfejsu do uzupelnienia i nikt go nie zasiewa, wiec
        // pusty oznacza „nie skonfigurowano”, a nie „nic nie wolno”. Wczesniej blokowal kazda
        // zmiane statusu u kazdego najemcy.
        if (!await transitionRepository.HasAnyAsync(tenantId, cancellationToken))
            return Result.Success();

        return Result.Failure(new Error("Task.TransitionNotAllowed",
            "Przejście między tymi statusami nie jest dozwolone."));
    }
}
