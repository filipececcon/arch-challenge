using ArchChallenge.CashFlow.Application.Abstractions.Queries;

namespace ArchChallenge.CashFlow.Application.Transactions.GetById;

public record GetTransactionByIdQuery(Guid Id, string UserId) : IQuery<GetTransactionByIdResult?>;
