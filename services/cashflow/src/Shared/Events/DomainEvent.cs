namespace ArchChallenge.CashFlow.Domain.Shared.Events;

public abstract record DomainEvent(object Payload) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public virtual string EventName => GetType().Name.Replace("Event", "");
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public object Payload { get; } = Payload;
}
