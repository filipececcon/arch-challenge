namespace ArchChallenge.CashFlow.Application.Abstractions.Behaviors;

/// <summary>
/// Preenche <see cref="IAuditable.UserId"/> e <see cref="IAuditable.OccurredAt"/> para
/// todo comando que implemente <see cref="IAuditable"/>, independente de como foi criado.
/// Deve ser o primeiro behavior no pipeline para garantir que os dados estejam disponíveis
/// nos behaviors e handlers subsequentes.
/// </summary>
public sealed class IdentityBehavior<TRequest, TResponse>(ICurrentUserAccessor currentUserAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditable
{
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.UserId))
            request.UserId = currentUserAccessor.UserId;

        if (request.OccurredAt == default)
            request.OccurredAt = DateTime.UtcNow;

        return next(cancellationToken);
    }
}
