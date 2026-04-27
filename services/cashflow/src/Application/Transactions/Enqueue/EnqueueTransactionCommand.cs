using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

/// <summary>
/// Fluxo de enfileiramento: valida a entrada e publica a mensagem no broker.
/// Sem UnitOfWork, sem Outbox, sem auditoria.
/// </summary>
public record EnqueueTransactionCommand(TransactionType Type, decimal Amount, string? Description)
    : EnqueueCommand<EnqueueTransactionResult>
{
    public Guid? IdempotencyKey { get; init; }

    public EnqueueTransactionMessage BuildMessage(Guid taskId) =>
        new(taskId, UserId, OccurredAt, Type, Amount, Description);
}
