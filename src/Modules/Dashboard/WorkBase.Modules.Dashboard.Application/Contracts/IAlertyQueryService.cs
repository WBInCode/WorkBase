using WorkBase.Modules.Dashboard.Application.Dtos;

namespace WorkBase.Modules.Dashboard.Application.Contracts;

public interface IAlertyQueryService
{
    /// <summary>
    /// Pozycje wymagajace uwagi. Zakres pracownikow przychodzi z zewnatrz — zapytanie samo
    /// niczego nie zawęża, bo nie zna uprawnien pytajacego.
    /// </summary>
    Task<List<AlertDto>> PobierzAsync(
        Guid tenantId,
        IReadOnlyList<Guid> pracownicyWZakresie,
        Guid? akceptantEmployeeId,
        int dniOczekiwaniaNaDecyzje,
        bool pokazujStawki,
        CancellationToken cancellationToken = default);
}
