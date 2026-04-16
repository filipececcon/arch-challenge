using System.Linq.Expressions;

namespace ArchChallenge.Dashboard.Domain.Shared.Criteria;

internal sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == source ? target : node;
}
