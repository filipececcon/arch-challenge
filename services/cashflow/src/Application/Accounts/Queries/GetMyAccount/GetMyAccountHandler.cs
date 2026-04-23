namespace ArchChallenge.CashFlow.Application.Accounts.Queries.GetMyAccount;

public sealed class GetMyAccountHandler(IReadRepository<Account> accountRepository)
    : IRequestHandler<GetMyAccountQuery, GetMyAccountResult?>
{
    public async Task<GetMyAccountResult?> Handle(GetMyAccountQuery request, CancellationToken cancellationToken)
    {
        var spec = new AccountByUserIdSpec(request.UserId);
        
        var account = await accountRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is null) return null;

        return new GetMyAccountResult(
            account.Id,
            account.UserId,
            account.Balance,
            account.Active,
            account.CreatedAt,
            account.UpdatedAt);
    }
}

