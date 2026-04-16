
namespace ArchChallenge.CashFlow.Application.Common.Behaviors;

/// <summary>
/// Executa após a validação: define metadados antes do handler para o <see cref="IAuditContext"/>.
/// A materialização no outbox ocorre na UoW (<c>SaveChanges</c>).
/// </summary>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditContext auditContext)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : class
{
    public Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (request is CommandBase cmd && request is IAuditable)
            auditContext.SetMetadata(cmd.UserId, cmd.OccurredAt);

        return next(cancellationToken);
    }
}
