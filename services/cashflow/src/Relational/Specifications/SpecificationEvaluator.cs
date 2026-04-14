using ArchChallenge.CashFlow.Domain.Shared.Specifications;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Specifications;

internal static class SpecificationEvaluator<T> where T : Entity
{
    public static IQueryable<T> GetQuery(IQueryable<T> query, ISpecification<T> spec)
    {
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip!.Value).Take(spec.Take!.Value);

        return query;
    }
}
