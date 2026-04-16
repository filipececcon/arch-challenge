using ArchChallenge.CashFlow.Domain.Shared.Logging;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Repositories;

public sealed class AuditOutboxRepository(CashFlowDbContext context) : IAuditOutboxRepository
{
    public async Task<IReadOnlyList<AuditEvent>> GetPendingAsync(
        int batchSize  = 50,
        int maxRetries = 5,
        CancellationToken cancellationToken = default)
        => await context.AuditOutboxEvents
            .TagWith(OutboxWorkerEfQueryTags.AuditPendingBatchQueryMarker)
            .Where(o => !o.Processed && o.RetryCount < maxRetries)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
