using ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDto?>;
