using System.Linq.Expressions;

namespace ArchChallenge.Dashboard.Domain.Shared.Criteria;

/// <summary>
/// Monta um predicado composto para consultas em documentos MongoDB.
/// </summary>
public sealed class QueryCriteriaBuilder<T> where T : class
{
    private Expression<Func<T, bool>>? _predicate;

    public QueryCriteriaBuilder<T> Where(Expression<Func<T, bool>> criterion)
    {
        _predicate = PredicateBuilder.And(_predicate, criterion);
        return this;
    }

    public QueryCriteriaBuilder<T> AndIf(bool condition, Expression<Func<T, bool>> criterion)
    {
        if (condition) Where(criterion);
        return this;
    }

    public QueryCriteriaBuilder<T> Or(Expression<Func<T, bool>> criterion)
    {
        _predicate = PredicateBuilder.Or(_predicate, criterion);
        return this;
    }

    public QueryCriteriaBuilder<T> OrIf(bool condition, Expression<Func<T, bool>> criterion)
    {
        if (condition) Or(criterion);
        return this;
    }

    public Expression<Func<T, bool>>? Build() => _predicate;
}
