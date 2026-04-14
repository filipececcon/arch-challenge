using System.Linq.Expressions;

namespace ArchChallenge.CashFlow.Domain.Shared.Query;

/// <summary>
/// Monta um predicado composto (<c>AND</c> / <c>OR</c> opcionais) para consultas em documentos ou entidades.
/// Exemplo:
/// <code>
/// var criteria = new QueryCriteriaBuilder&lt;TransactionDocument&gt;()
///     .Where(d => d.Active)
///     .AndIf(request.MinAmount is { } min, d => d.Amount &gt;= min)
///     .Build();
/// await repository.ListAsync(predicate: criteria, cancellationToken: ct);
/// </code>
/// </summary>
public sealed class QueryCriteriaBuilder<T> where T : class
{
    private Expression<Func<T, bool>>? _predicate;

    /// <summary>Acrescenta critério com <c>AND</c>.</summary>
    public QueryCriteriaBuilder<T> Where(Expression<Func<T, bool>> criterion)
    {
        _predicate = PredicateBuilder.And(_predicate, criterion);
        return this;
    }

    /// <summary>Acrescenta critério com <c>AND</c> apenas se <paramref name="condition"/> for verdadeiro.</summary>
    public QueryCriteriaBuilder<T> AndIf(bool condition, Expression<Func<T, bool>> criterion)
    {
        if (condition)
            Where(criterion);
        return this;
    }

    /// <summary>Acrescenta critério com <c>OR</c> (útil para alternativas).</summary>
    public QueryCriteriaBuilder<T> Or(Expression<Func<T, bool>> criterion)
    {
        _predicate = PredicateBuilder.Or(_predicate, criterion);
        return this;
    }

    /// <summary>Acrescenta critério com <c>OR</c> apenas se <paramref name="condition"/> for verdadeiro.</summary>
    public QueryCriteriaBuilder<T> OrIf(bool condition, Expression<Func<T, bool>> criterion)
    {
        if (condition)
            Or(criterion);
        return this;
    }

    /// <summary>
    /// Predicado final; <see langword="null"/> significa “sem filtro” (todos os documentos).
    /// </summary>
    public Expression<Func<T, bool>>? Build() => _predicate;
}
