using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Modules.Organization.Application.Contracts;

public interface IListaKontrolnaRepository
{
    /// <summary>Wszystkie listy firmy z pozycjami — administrator widzi także wyłączone.</summary>
    Task<List<ListaKontrolna>> PobierzWszystkieAsync(CancellationToken cancellationToken = default);

    Task<ListaKontrolna?> PobierzAsync(Guid id, CancellationToken cancellationToken = default);

    Task DodajAsync(ListaKontrolna lista, CancellationToken cancellationToken = default);
}
