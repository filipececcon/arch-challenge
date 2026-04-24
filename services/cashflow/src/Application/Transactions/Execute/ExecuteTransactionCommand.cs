using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Execute;

public record ExecuteTransactionCommand(
    Guid TaskId,
    TransactionType Type,
    decimal Amount,
    string? Description
) : IAuditable, IRequest
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
