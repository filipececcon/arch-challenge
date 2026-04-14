namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdResult(
    Guid Id,
    string Type,
    decimal Amount,
    string? Description,
    DateTime CreatedAt,
    bool Active)
{
}