using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public record EnqueueTransactionMessage(
    Guid TaskId,
    string UserId,
    DateTime OccurredAt,
    TransactionType Type,
    decimal Amount,
    string? Description);
