namespace ArchChallenge.CashFlow.Domain.Events;

public record TransactionDoneEvent(object Payload) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public string EventName { get; } = "TransactionCreated";
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public object Payload { get; } = Payload;
}
