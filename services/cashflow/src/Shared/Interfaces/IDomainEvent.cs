namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Interface marcadora para todos os eventos de domínio.
/// Camada pura — sem dependência de frameworks externos.
///
/// A ponte com o sistema de notificações do MediatR é feita pela camada
/// Application via <c>DomainEventNotification&lt;TEvent&gt;</c>.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    string EventName { get; }
    DateTime OccurredAt { get; }
    public object Payload { get; }
}
