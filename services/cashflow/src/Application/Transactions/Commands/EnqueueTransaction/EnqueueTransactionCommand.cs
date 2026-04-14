using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;

public record EnqueueTransactionCommand(TransactionType Type, decimal Amount, string? Description)
    : IEnqueueCommand<EnqueueTransactionMessage>
{
    public Guid? IdempotencyKey { get; init; }

    public EnqueueTransactionMessage BuildMessage(Guid taskId) => new(taskId, Type, Amount, Description);
}
