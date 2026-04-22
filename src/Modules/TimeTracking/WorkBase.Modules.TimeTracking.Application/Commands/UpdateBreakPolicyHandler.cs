using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class UpdateBreakPolicyHandler(IBreakPolicyRepository breakPolicyRepository)
    : ICommandHandler<UpdateBreakPolicyCommand>
{
    public async Task<Result> Handle(UpdateBreakPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await breakPolicyRepository.GetByIdAsync(
            request.TenantId, request.PolicyId, cancellationToken);

        if (policy is null)
            return Result.Failure(Error.NotFound(
                "BreakPolicy.NotFound",
                "Nie znaleziono polityki przerw."));

        policy.Update(
            request.Name,
            request.MaxPerDay,
            request.MaxMinutesPerBreak,
            request.MaxMinutesPerDay,
            request.IsActive);

        breakPolicyRepository.Update(policy);

        return Result.Success();
    }
}
