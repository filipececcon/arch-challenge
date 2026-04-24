using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Transactions.GetById;

public record GetTransactionByIdResult(
    Guid Id,
    Guid AccountId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime CreatedAt,
    bool Active) : IResponse;
