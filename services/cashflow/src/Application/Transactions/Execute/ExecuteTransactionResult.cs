using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Transactions.Execute;

public record ExecuteTransactionResult(
    Guid     Id,
    Guid     AccountId,
    string   Type,
    decimal  Amount,
    decimal  BalanceAfter,
    string?  Description,
    DateTime CreatedAt) : IResponse;
