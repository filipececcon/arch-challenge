namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id, string UserId) : IRequest<GetTransactionByIdResult?>;
