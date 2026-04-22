namespace ArchChallenge.CashFlow.Domain.Specifications;

public sealed class AccountByUserIdSpec : Specification<Account>
{
    public AccountByUserIdSpec(string userId) =>
        AddCriteria(a => a.UserId == userId);
}
