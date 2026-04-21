using Microsoft.EntityFrameworkCore;
using WorkBase.Shared.Persistence;

namespace WorkBase.Infrastructure.Persistence;

/// <summary>
/// EF Core-specific specification extensions (Include support).
/// </summary>
public static class EfSpecificationExtensions
{
    public static IQueryable<T> ApplyWithIncludes<T>(this IQueryable<T> query, ISpecification<T> spec)
        where T : class
    {
        query = query.Where(spec.Criteria);

        foreach (var include in spec.Includes)
        {
            query = query.Include(include);
        }

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
