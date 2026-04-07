using ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public class GetTransactionByIdHandler(IReadRepository<Transaction> repository)
    : IRequestHandler<GetTransactionByIdQuery, TransactionDto?>
{
    public async Task<TransactionDto?> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var spec = new TransactionByIdSpec(request.Id);
        var transaction = await repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (transaction is null)
            return null;

        return new TransactionDto(
            transaction.Id,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.Description,
            transaction.CreatedAt,
            transaction.Active);
    }
}
