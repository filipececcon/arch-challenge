using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Queries.GetMyAccount;

public record GetMyAccountResult(
    Guid     Id,
    string   UserId,
    decimal  Balance,
    bool     Active,
    DateTime CreatedAt,
    DateTime UpdatedAt) : IResponse;

