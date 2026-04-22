namespace ArchChallenge.CashFlow.Domain.Shared.Entities;

/// <summary>
/// Entidade unificada de outbox transacional.
///
/// Três destinos possíveis, todos gravados atomicamente na mesma transação:
/// <list type="bullet">
///   <item><see cref="OutboxTarget.Mongo"/> — projeção no read model (MongoDB).</item>
///   <item><see cref="OutboxTarget.Audit"/> — auditoria imutável (immudb).</item>
///   <item><see cref="OutboxTarget.Events"/> — evento de integração publicado no broker.</item>
/// </list>
///
/// Referência: https://microservices.io/patterns/data/transactional-outbox.html
/// </summary>
public class Outbox : Entity
{
    /// <summary>Nome do tipo do evento (ex.: "TransactionCreated").</summary>
    public string Kind { get; private set; } = null!;

    /// <summary>Payload serializado em JSON com os dados do evento.</summary>
    public string Payload { get; private set; } = null!;

    /// <summary>Destino de processamento: projeção (MongoDB) ou auditoria (immudb).</summary>
    public OutboxTarget Target { get; private set; }

    /// <summary>Indica se o evento já foi processado com sucesso.</summary>
    public bool Processed { get; private set; }

    /// <summary>Momento em que o evento foi processado com sucesso.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>Número de tentativas falhas de processamento (limite configurável).</summary>
    public int RetryCount { get; private set; }

    // EF Core
    private Outbox() { }

    private Outbox(string kind, string payload, OutboxTarget target)
    {
        Kind      = kind;
        Payload   = payload;
        Target    = target;
        Processed = false;
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>Cria um registro de outbox para projeção no read model (MongoDB).</summary>
    public static Outbox ForMongo(string kind, string payload)
        => new(kind, payload, OutboxTarget.Mongo);

    /// <summary>Cria um registro de outbox para auditoria imutável (immudb).</summary>
    public static Outbox ForAudit(string kind, string payload)
        => new(kind, payload, OutboxTarget.Audit);

    /// <summary>Cria um registro de outbox para publicação de evento de integração no broker.</summary>
    public static Outbox ForEvents(string kind, string payload)
        => new(kind, payload, OutboxTarget.Events);

    // ── Behaviour ────────────────────────────────────────────────────────────

    /// <summary>Marca o registro como processado com sucesso.</summary>
    public void MarkProcessed()
    {
        Processed   = true;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>Incrementa o contador de tentativas falhas.</summary>
    public void IncrementRetry() => RetryCount++;
}