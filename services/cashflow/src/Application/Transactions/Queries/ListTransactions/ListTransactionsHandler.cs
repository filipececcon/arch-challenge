namespace ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;

public class ListTransactionsHandler(IReadRepository<Transaction> repository)
    : IRequestHandler<ListTransactionsQuery, IReadOnlyList<TransactionDto>>
{
    public async Task<IReadOnlyList<TransactionDto>> Handle(ListTransactionsQuery request, CancellationToken cancellationToken)
    {
        var spec = new TransactionsOrderedByDateSpec();
        var transactions = await repository.ListAsync(spec, cancellationToken);

        return transactions
            .Select(t => new TransactionDto(
                t.Id,
                t.Type.ToString(),
                t.Amount,
                t.Description,
                t.CreatedAt,
                t.Active))
            .ToList()
            .AsReadOnly();
    }
}
