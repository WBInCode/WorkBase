using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class ReviewAnomalyHandler(ITimeAnomalyRepository anomalyRepository)
    : ICommandHandler<ReviewAnomalyCommand>
{
    public async Task<Result> Handle(ReviewAnomalyCommand request, CancellationToken cancellationToken)
    {
        var anomaly = await anomalyRepository.GetByIdAsync(
            request.TenantId, request.AnomalyId, cancellationToken);

        if (anomaly is null)
            return Result.Failure(Error.NotFound(
                "Anomaly.NotFound",
                "Anomalia nie została znaleziona."));

        anomaly.Review(request.ReviewedBy);
        anomalyRepository.Update(anomaly);

        return Result.Success();
    }
}
