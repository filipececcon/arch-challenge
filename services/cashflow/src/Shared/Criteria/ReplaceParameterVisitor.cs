namespace ArchChallenge.CashFlow.Domain.Shared.Criteria;

internal sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target)
    : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node)
        => node == source ? target : node;
}
