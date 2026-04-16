using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;

public record EnqueueTransaction(TransactionType Type, decimal Amount, string? Description)
    : CommandBase, IEnqueueCommand<EnqueueTransactionMessage>
{
    public Guid? IdempotencyKey { get; init; }

    public EnqueueTransactionMessage BuildMessage(Guid taskId) =>
        new(taskId, UserId, OccurredAt, Type, Amount, Description);
}
