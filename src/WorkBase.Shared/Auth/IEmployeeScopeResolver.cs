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

    /// <summary>
    /// Wylicza pracownikow widocznych dla uzytkownika bez podawania listy kandydatow.
    /// Zwraca <c>null</c>, gdy zakres nie ogranicza niczego (Branch/Organization) — wtedy
    /// filtrowanie jest zbedne i nie oplaca sie materializowac calej firmy. Pusty zbior
    /// oznacza „nic nie widzi", a NIE „widzi wszystko".
    /// </summary>
    Task<IReadOnlySet<Guid>?> GetVisibleEmployeeIdsAsync(
        Guid userId,
        Guid tenantId,
        Guid? callerEmployeeId,
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
