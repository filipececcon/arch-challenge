namespace ArchChallenge.CashFlow.Domain.Shared.Events;

/// <summary>
/// Representa um evento pendente de processamento armazenado no PostgreSQL
/// como parte do padrão Transactional Outbox.
///
/// É persistido na mesma transação da entidade principal, garantindo
/// atomicidade sem necessidade de 2PC (Two-Phase Commit) entre os bancos.
///
/// O <see cref="Workers.OutboxWorkerService"/> consome esses registros e
/// os sincroniza com o MongoDB (read model), marcando-os como processados.
///
/// Referência: https://microservices.io/patterns/data/transactional-outbox.html
/// </summary>
public class OutboxEvent : Entity
{
    /// <summary>Nome do tipo do evento (ex: "TransactionRegistered").</summary>
    public string EventType { get; private set; } = null!;

    /// <summary>Payload serializado em JSON com os dados do evento.</summary>
    public string Payload { get; private set; } = null!;

    /// <summary>Indica se o evento já foi sincronizado com o MongoDB.</summary>
    public bool Processed { get; private set; }

    /// <summary>Momento em que o evento foi processado com sucesso.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>Número de tentativas falhas de processamento (limite: 5).</summary>
    public int RetryCount { get; private set; }

    // EF Core
    private OutboxEvent() { }

    public OutboxEvent(string eventType, string payload)
    {
        EventType = eventType;
        Payload   = payload;
        Processed = false;
    }

    /// <summary>Marca o evento como processado com sucesso.</summary>
    public void MarkProcessed()
    {
        Processed    = true;
        ProcessedAt  = DateTime.UtcNow;
    }

    /// <summary>Incrementa o contador de tentativas falhas.</summary>
    public void IncrementRetry() => RetryCount++;
}

