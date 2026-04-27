using ArchChallenge.CashFlow.Application.Abstractions.Outbox;
using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Abstractions.Behaviors;

/// <summary>
/// Persiste as entradas de outbox registradas pelo handler via <see cref="IOutboxContext"/>.
/// Roda dentro do <see cref="UnitOfWorkBehavior{TCommand,TResult}"/> para que as entradas
/// sejam salvas na mesma transação do agregado.
/// Aplica-se a todo comando do fluxo síncrono (<see cref="ISyncCommand"/>).
/// </summary>
public sealed class OutboxBehavior<TCommand, TResult>(
    IOutboxContext outboxContext,
    IOutboxRepository outboxRepository)
    : IPipelineBehavior<TCommand, TResult>
    where TCommand : ISyncCommand
{
    public async Task<TResult> Handle(TCommand command, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        var result = await next(cancellationToken);

        if (result is IResult { IsFailure: true })
            return result;

        foreach (var entry in outboxContext.Entries)
            await outboxRepository.AddAsync(entry, cancellationToken);

        return result;
    }
}
