namespace ArchChallenge.CashFlow.Domain.Shared.Events;

/// <summary>
/// Evento de outbox transacional para sincronização com o MongoDB (read model).
///
/// É persistido na mesma transação da entidade principal, garantindo
/// atomicidade sem necessidade de 2PC (Two-Phase Commit) entre os bancos.
/// O <c>OutboxWorkerService</c> consome esses registros e os projeta no MongoDB,
/// marcando-os como processados.
/// </summary>
public sealed class OutboxEvent : EventBase
{
    // EF Core
    private OutboxEvent() { }

    public OutboxEvent(string eventType, string payload) : base(eventType, payload) { }
}

