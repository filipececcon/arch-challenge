using ArchChallenge.CashFlow.Application.Abstractions.Queries;

namespace ArchChallenge.CashFlow.Application.Accounts.GetMyAccount;

public record GetMyAccountQuery(string UserId) : IQuery<GetMyAccountResult?>;
