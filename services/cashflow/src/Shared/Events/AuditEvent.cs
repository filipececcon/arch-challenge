namespace ArchChallenge.CashFlow.Domain.Shared.Events;

/// <summary>
/// Evento de outbox transacional para auditoria imutável (immudb).
/// O <c>AuditOutboxWorkerService</c> consome esses registros e os persiste no immudb,
/// marcando-os como processados.
/// </summary>
public sealed class AuditEvent : EventBase
{
    // EF Core
    private AuditEvent() { }

    public AuditEvent(string eventType, string payload) : base(eventType, payload) { }
}
