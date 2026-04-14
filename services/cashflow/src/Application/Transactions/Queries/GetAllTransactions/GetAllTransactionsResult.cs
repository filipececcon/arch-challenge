using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;

public record GetAllTransactionsResult(List<GetTransactionByIdResult> list)
{
    private List<GetTransactionByIdResult> list { get; init; } = list;
    
    public IReadOnlyCollection<GetTransactionByIdResult> Transactions => list.AsReadOnly();
}