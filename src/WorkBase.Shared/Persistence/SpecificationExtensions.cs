namespace WorkBase.Shared.Persistence;

/// <summary>
/// Applies specification criteria, ordering, and paging to IQueryable (no EF Core dependency).
/// For Include support, use the EF Core extension in WorkBase.Infrastructure.
/// </summary>
public static class SpecificationExtensions
{
    public static IQueryable<T> Apply<T>(this IQueryable<T> query, ISpecification<T> spec)
        where T : class
    {
        query = query.Where(spec.Criteria);

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);

        if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.Skip.HasValue)
            query = query.Skip(spec.Skip.Value);

        if (spec.Take.HasValue)
            query = query.Take(spec.Take.Value);

        return query;
    }
}
