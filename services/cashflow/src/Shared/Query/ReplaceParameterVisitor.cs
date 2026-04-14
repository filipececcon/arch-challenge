using System.Linq.Expressions;

namespace ArchChallenge.CashFlow.Domain.Shared.Query;

internal sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == source ? target : node;
}
