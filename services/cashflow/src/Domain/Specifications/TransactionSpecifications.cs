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
