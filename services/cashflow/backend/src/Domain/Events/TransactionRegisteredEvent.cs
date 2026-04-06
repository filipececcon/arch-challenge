using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;

namespace ArchChallenge.CashFlow.Domain.Events;

public record TransactionRegisteredEvent(
    Guid TransactionId,
    TransactionType Type,
    decimal Amount,
    string? Description) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventType { get; } = "TransactionRegistered";
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
