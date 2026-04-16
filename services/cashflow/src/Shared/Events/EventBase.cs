namespace ArchChallenge.CashFlow.Domain.Shared.Events;

/// <summary>
/// Base comum para eventos de outbox transacional (<see cref="OutboxEvent"/> e <see cref="AuditEvent"/>).
///
/// Concentra as propriedades e o comportamento operacional compartilhados:
/// controle de processamento, payload serializado e retry. Subclasses apenas
/// diferenciam o destino do processamento (MongoDB vs immudb) por meio do
/// repositório e do worker correspondentes.
///
/// Referência: https://microservices.io/patterns/data/transactional-outbox.html
/// </summary>
public abstract class EventBase : Entity
{
    /// <summary>Nome do tipo do evento (ex.: "TransactionCreated").</summary>
    public string EventType { get; private set; } = null!;

    /// <summary>Payload serializado em JSON com os dados do evento.</summary>
    public string Payload { get; private set; } = null!;

    /// <summary>Indica se o evento já foi processado com sucesso.</summary>
    public bool Processed { get; private set; }

    /// <summary>Momento em que o evento foi processado com sucesso.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>Número de tentativas falhas de processamento (limite: 5).</summary>
    public int RetryCount { get; private set; }

    // EF Core
    protected EventBase() { }

    protected EventBase(string eventType, string payload)
    {
        EventType = eventType;
        Payload   = payload;
        Processed = false;
    }

    /// <summary>Marca o evento como processado com sucesso.</summary>
    public void MarkProcessed()
    {
        Processed   = true;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>Incrementa o contador de tentativas falhas.</summary>
    public void IncrementRetry() => RetryCount++;
}
