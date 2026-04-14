namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id) : IRequest<GetTransactionByIdResult?>;
