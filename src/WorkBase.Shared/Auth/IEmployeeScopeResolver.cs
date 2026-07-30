namespace WorkBase.Shared.Auth;

/// <summary>
/// Rozstrzyga, czyje dane widzi użytkownik: własne, podwładnych czy całej firmy — na podstawie
/// zakresu danych (DataScope) przypisanego do jego ról.
/// </summary>
public interface IEmployeeScopeResolver
{
    Task<bool> CanAccessEmployeeAsync(
        Guid userId,
        Guid tenantId,
        Guid? callerEmployeeId,
        Guid targetEmployeeId,
        string module,
        CancellationToken ct = default);

    Task<IReadOnlySet<Guid>> FilterAccessibleAsync(
        Guid userId,
        Guid tenantId,
        Guid? callerEmployeeId,
        IReadOnlyCollection<Guid> targetEmployeeIds,
        string module,
        CancellationToken ct = default);
}
