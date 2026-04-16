using ArchChallenge.CashFlow.Domain.Shared.Events;

namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Contrato de persistência do <see cref="OutboxEvent"/>.
/// Mantém a Application desacoplada da implementação EF Core,
/// permitindo que o handler injete o repositório sem conhecer
/// detalhes de infraestrutura.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>Agenda um evento para processamento futuro pelo worker.</summary>
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna os próximos <paramref name="batchSize"/> eventos não processados,
    /// com menos de <paramref name="maxRetries"/> tentativas falhas, ordenados por data de criação.
    /// </summary>
    Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(
        int batchSize           = 50,
        int maxRetries          = 5,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste as alterações (markProcessed / incrementRetry).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se existe evento não processado do <paramref name="eventType"/> cujo
    /// <see cref="OutboxEvent.Payload"/> referencia o agregado (ex.: mesmo <c>id</c> no JSON).
    /// Usado em leituras híbridas Mongo → outbox → relacional.
    /// </summary>
    Task<bool> HasPendingForAggregateAsync(
        string            eventType,
        Guid              aggregateId,
        int               maxRetries        = 5,
        CancellationToken cancellationToken = default);
}


