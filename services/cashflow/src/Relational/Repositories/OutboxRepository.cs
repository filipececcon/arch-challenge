using ArchChallenge.CashFlow.Domain.Shared.Logging;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Repositories;

/// <summary>
/// Repositório EF Core para o <see cref="OutboxEvent"/>.
/// Opera diretamente sobre o <see cref="CashFlowDbContext"/> compartilhado
/// no escopo da requisição, permitindo que <c>AddAsync</c> participe da
/// mesma transação aberta pelo <c>UnitOfWork</c>.
/// </summary>
public class OutboxRepository(CashFlowDbContext context) : IOutboxRepository
{
    public async Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
        => await context.OutboxEvents.AddAsync(outboxEvent, cancellationToken);

    public async Task<IReadOnlyList<OutboxEvent>> GetPendingAsync(
        int batchSize           = 50,
        int maxRetries          = 5,
        CancellationToken cancellationToken = default)
        => await context.OutboxEvents
            .TagWith(OutboxWorkerEfQueryTags.PendingBatchQueryMarker)
            .Where(o => !o.Processed && o.RetryCount < maxRetries)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<bool> HasPendingForAggregateAsync(
        string            eventType,
        Guid              aggregateId,
        int               maxRetries        = 5,
        CancellationToken cancellationToken = default)
    {
        var idInPayload = aggregateId.ToString("D");
        return await context.OutboxEvents
            .AsNoTracking()
            .AnyAsync(
                o => !o.Processed
                     && o.RetryCount < maxRetries
                     && o.EventType == eventType
                     && o.Payload.Contains(idInPayload),
                cancellationToken);
    }
}


