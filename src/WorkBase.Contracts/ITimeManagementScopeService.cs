namespace WorkBase.Contracts;

/// <summary>
/// Rozstrzyga, czyją ewidencję czasu pracy może modyfikować zalogowany użytkownik.
/// HR/Admin (time.manage) obejmują całą firmę, kierownik (time.edit) tylko siebie i swój zespół.
/// </summary>
public interface ITimeManagementScopeService
{
    Task<bool> CanManageEmployeeTimeAsync(
        Guid userId,
        Guid tenantId,
        Guid targetEmployeeId,
        CancellationToken cancellationToken = default);
}
