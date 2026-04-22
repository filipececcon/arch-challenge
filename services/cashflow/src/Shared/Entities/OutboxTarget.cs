namespace ArchChallenge.CashFlow.Domain.Shared.Entities;

/// <summary>
/// Destino de processamento de um registro de outbox.
/// </summary>
public enum OutboxTarget
{
    /// <summary>Projeção no read model (MongoDB).</summary>
    Mongo = 0,

    /// <summary>Auditoria imutável (immudb).</summary>
    Audit = 1,

    /// <summary>Evento de integração publicado no broker de mensagens (RabbitMQ).</summary>
    Events = 2
}
