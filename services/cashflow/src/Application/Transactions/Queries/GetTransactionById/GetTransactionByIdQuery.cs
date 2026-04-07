using ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDto?>;
