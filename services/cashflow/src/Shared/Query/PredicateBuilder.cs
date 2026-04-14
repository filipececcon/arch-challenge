using System.Linq.Expressions;

namespace ArchChallenge.CashFlow.Domain.Shared.Query;

/// <summary>
/// Combina expressões <see cref="Expression{TDelegate}"/> com um único parâmetro,
/// sem <c>Expression.Invoke</c>, para compatibilidade com provedores LINQ (ex.: MongoDB.Driver).
/// </summary>
public static class PredicateBuilder
{
    public static Expression<Func<T, bool>>? And<T>(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right)
    {
        if (left is null)  return right;
        if (right is null) return left;

        var param = Expression.Parameter(typeof(T), "x");
        var leftBody  = new ReplaceParameterVisitor(left.Parameters[0], param).Visit(left.Body);
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], param).Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(leftBody!, rightBody!),
            param);
    }

    public static Expression<Func<T, bool>>? Or<T>(
        Expression<Func<T, bool>>? left,
        Expression<Func<T, bool>>? right)
    {
        if (left is null)  return right;
        if (right is null) return left;

        var param = Expression.Parameter(typeof(T), "x");
        var leftBody  = new ReplaceParameterVisitor(left.Parameters[0], param).Visit(left.Body);
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], param).Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(leftBody!, rightBody!),
            param);
    }

    public static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> expression)
    {
        var param = expression.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(expression.Body), param);
    }
}
