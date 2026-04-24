using ArchChallenge.CashFlow.Application.Common.Responses;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;

public record CreateAccountResult(
    Guid     Id,
    string   UserId,
    decimal  Balance,
    DateTime CreatedAt) : IResponse;

