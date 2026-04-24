using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public record EnqueueTransactionCommand(TransactionType Type, decimal Amount, string? Description)
    : IAuditable, IRequest<EnqueueResult>
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? IdempotencyKey { get; init; }

    public EnqueueTransactionMessage BuildMessage(Guid taskId) =>
        new(taskId, UserId, OccurredAt, Type, Amount, Description);
}
