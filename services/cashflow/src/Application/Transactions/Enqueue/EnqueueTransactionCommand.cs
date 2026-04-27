using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

/// <summary>
/// Fluxo de enfileiramento: valida a entrada e publica a mensagem no broker.
/// Sem UnitOfWork, sem Outbox, sem auditoria.
/// O <c>EnqueueBehavior</c> cuida do TaskId, idempotência e status no cache.
/// </summary>
public record EnqueueTransactionCommand(TransactionType Type, decimal Amount, string? Description)
    : EnqueueCommand<EnqueueTransactionResult>
{
    public EnqueueTransactionMessage BuildMessage() =>
        new(TaskId, UserId, OccurredAt, Type, Amount, Description);
}
