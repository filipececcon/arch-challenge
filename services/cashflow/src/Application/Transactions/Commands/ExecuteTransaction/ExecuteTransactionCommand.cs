using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public record ExecuteTransactionCommand(
    Guid TaskId,
    TransactionType Type,
    decimal Amount,
    string? Description) : IRequest;
