using ArchChallenge.CashFlow.Application.Abstractions.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.GetMyAccount;

public record GetMyAccountResult(
    Guid     Id,
    string   UserId,
    decimal  Balance,
    bool     Active,
    DateTime CreatedAt,
    DateTime UpdatedAt) : IResponse;
