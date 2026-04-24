using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Create;

public record CreateAccountResult(
    Guid     Id,
    string   UserId,
    decimal  Balance,
    DateTime CreatedAt) : IResponse;
