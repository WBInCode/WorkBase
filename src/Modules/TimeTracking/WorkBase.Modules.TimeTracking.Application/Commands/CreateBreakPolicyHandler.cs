using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class CreateBreakPolicyHandler(IBreakPolicyRepository breakPolicyRepository)
    : ICommandHandler<CreateBreakPolicyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateBreakPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = BreakPolicy.Create(
            request.TenantId,
            request.Name,
            request.BreakType,
            request.MaxPerDay,
            request.MaxMinutesPerBreak,
            request.MaxMinutesPerDay,
            request.IsActive);

        await breakPolicyRepository.AddAsync(policy, cancellationToken);

        return policy.Id;
    }
}
