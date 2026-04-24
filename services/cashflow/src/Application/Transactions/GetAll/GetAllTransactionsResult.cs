using ArchChallenge.CashFlow.Application.Abstractions.Responses;
using ArchChallenge.CashFlow.Application.Transactions.GetById;

namespace ArchChallenge.CashFlow.Application.Transactions.GetAll;

public sealed record GetAllTransactionsResult(IReadOnlyCollection<GetTransactionByIdResult> Transactions)
    : IResponse;
