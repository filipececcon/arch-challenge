namespace ArchChallenge.CashFlow.Application.Transactions.Queries.ListTransactions;

public record ListTransactionsQuery : IRequest<IReadOnlyList<TransactionDto>>;

public record TransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string? Description,
    DateTime CreatedAt,
    bool Active);
