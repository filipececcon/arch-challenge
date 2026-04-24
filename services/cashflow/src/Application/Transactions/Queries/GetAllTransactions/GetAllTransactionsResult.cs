using ArchChallenge.CashFlow.Application.Common.Responses;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;

public sealed record GetAllTransactionsResult(IReadOnlyCollection<GetTransactionByIdResult> Transactions)
    : IResponse;