using ArchChallenge.CashFlow.Application.Abstractions.Queries;

namespace ArchChallenge.CashFlow.Application.Transactions.GetAll;

public record GetAllTransactionsQuery(
    string     UserId,
    string?    Type         = null,
    bool?      Active       = null,
    decimal?   MinAmount    = null,
    decimal?   MaxAmount    = null,
    DateTime?  CreatedFrom  = null,
    DateTime?  CreatedTo    = null)
    : IQuery<GetAllTransactionsResult>;
