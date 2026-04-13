using ArchChallenge.CashFlow.Domain.Shared.Events;

namespace ArchChallenge.CashFlow.Domain.Events;

public record TransactionProcessedEvent(object Payload) : DomainEvent(Payload)
{
    private const string _eventName = "TransactionProcessed"; // ponto único de verdade
    public override string EventName => _eventName;
}
