using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;

public record CreateTransactionCommand(TransactionType Type, decimal Amount, string? Description) 
    : IEnqueueCommand<CreateTransactionMessage>
{
    public CreateTransactionMessage BuildMessage(Guid taskId) => new(taskId, Type, Amount, Description);
}
