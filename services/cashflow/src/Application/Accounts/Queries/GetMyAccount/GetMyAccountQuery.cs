namespace ArchChallenge.CashFlow.Application.Accounts.Queries.GetMyAccount;

public record GetMyAccountQuery(string UserId) : IRequest<GetMyAccountResult?>;

