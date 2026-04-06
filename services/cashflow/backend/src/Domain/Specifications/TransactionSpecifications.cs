using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Specifications;

namespace ArchChallenge.CashFlow.Domain.Specifications;

public class TransactionByIdSpec : Specification<Transaction>
{
    public TransactionByIdSpec(Guid id) =>
        AddCriteria(t => t.Id == id);
}

public class TransactionsOrderedByDateSpec : Specification<Transaction>
{
    public TransactionsOrderedByDateSpec() =>
        AddOrderByDescending(t => t.CreatedAt);
}
