namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Contrato de persistência do <see cref="Outbox"/>.
/// Mantém a Application desacoplada da implementação EF Core,
/// permitindo que handlers e workers injetem o repositório sem conhecer
/// detalhes de infraestrutura.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>Agenda um registro para processamento futuro pelo worker.</summary>
    Task AddAsync(Outbox entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna os próximos <paramref name="batchSize"/> registros não processados
    /// do <paramref name="target"/> especificado, com menos de <paramref name="maxRetries"/>
    /// tentativas falhas, ordenados por data de criação.
    /// </summary>
    Task<IReadOnlyList<Outbox>> GetPendingAsync(
        OutboxTarget      target,
        int               batchSize           = 50,
        int               maxRetries          = 5,
        CancellationToken cancellationToken   = default);

    /// <summary>Retorna o registro com o <paramref name="id"/> especificado, ou <c>null</c> se não encontrado.</summary>
    Task<Outbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persiste as alterações (MarkProcessed / IncrementRetry).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica se existe registro não processado do <paramref name="kind"/> cujo
    /// <see cref="Outbox.Payload"/> referencia o agregado (ex.: mesmo <c>id</c> no JSON).
    /// Usado em leituras híbridas Mongo → outbox → relacional.
    /// </summary>
    Task<bool> HasPendingForAggregateAsync(
        string            kind,
        Guid              aggregateId,
        int               maxRetries        = 5,
        CancellationToken cancellationToken = default);
}
