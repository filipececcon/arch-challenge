using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;

public record EnqueueTransactionMessage(
    Guid TaskId,
    string UserId,
    DateTime OccurredAt,
    TransactionType Type,
    decimal Amount,
    string? Description);
