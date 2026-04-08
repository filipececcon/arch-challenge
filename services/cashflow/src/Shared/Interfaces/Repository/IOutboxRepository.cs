using ArchChallenge.CashFlow.Domain.Shared.Events;

namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;

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
    /// com menos de 5 tentativas falhas, ordenados por data de criação.
    /// </summary>
    Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(
        int batchSize           = 50,
        CancellationToken cancellationToken = default);

    /// <summary>Persiste as alterações (markProcessed / incrementRetry).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}


