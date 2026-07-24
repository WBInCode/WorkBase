namespace WorkBase.Contracts.Ecosystem;

public interface IEcosystemSyncScheduler
{
    void Enqueue(Guid tenantId, Guid employeeId);
}