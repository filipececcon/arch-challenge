using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;

public record CreateTransactionMessage(
    Guid TaskId,
    TransactionType Type,
    decimal Amount,
    string? Description);
