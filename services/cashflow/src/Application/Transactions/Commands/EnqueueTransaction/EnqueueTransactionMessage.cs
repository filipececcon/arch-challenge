using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;

public record EnqueueTransactionMessage(
    Guid TaskId,
    TransactionType Type,
    decimal Amount,
    string? Description);
