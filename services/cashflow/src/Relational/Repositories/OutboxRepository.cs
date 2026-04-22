using ArchChallenge.CashFlow.Domain.Shared.Logging;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Repositories;

/// <summary>
/// Repositório EF Core para o <see cref="Outbox"/>.
/// Opera diretamente sobre o <see cref="CashFlowDbContext"/> compartilhado
/// no escopo da requisição, permitindo que <c>AddAsync</c> participe da
/// mesma transação aberta pelo <c>UnitOfWork</c>.
///
/// <para><b>Concorrência:</b> <see cref="GetPendingAsync"/> usa <c>SELECT … FOR UPDATE SKIP LOCKED</c>
/// dentro de uma transação aberta pelo worker, garantindo que réplicas concorrentes
/// do mesmo worker nunca processem o mesmo lote (at-most-once locking, at-least-once delivery
/// combinada com idempotência no destino).</para>
/// </summary>
public class OutboxRepository(CashFlowDbContext context) : IOutboxRepository
{
    public async Task AddAsync(Outbox entity, CancellationToken cancellationToken = default)
        => await context.Outboxes.AddAsync(entity, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Deve ser chamado dentro de uma transação aberta (ex.: <c>BeginTransactionAsync</c> no worker)
    /// para que o <c>FOR UPDATE SKIP LOCKED</c> mantenha o lock até o <c>COMMIT</c> do ciclo.
    /// </remarks>
    public async Task<IReadOnlyList<Outbox>> GetPendingAsync(
        OutboxTarget      target,
        int               batchSize         = 50,
        int               maxRetries        = 5,
        CancellationToken cancellationToken = default)
    {
        var targetStr = target.ToString();
        
        return await context.Outboxes
            .FromSql($"""
                SELECT * FROM outbox."TB_OUTBOX"
                WHERE "ST_PROCESSED"    = false
                  AND "DS_TARGET"       = {targetStr}
                  AND "NR_RETRY_COUNT"  < {maxRetries}
                ORDER BY "DT_CREATED_AT"
                LIMIT    {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .TagWith(OutboxWorkerEfQueryTags.PendingBatchQueryMarker)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Outbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Outboxes
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Fragilidade conhecida:</b> usa <c>LIKE '%{aggregateId}%'</c> sobre o payload JSON,
    /// o que pode produzir falsos positivos se o GUID aparecer em outro campo.
    /// Melhoria futura: adicionar coluna <c>AggregateId</c> indexada ou usar JSON path no Postgres.
    /// </remarks>
    public async Task<bool> HasPendingForAggregateAsync(
        string            kind,
        Guid              aggregateId,
        int               maxRetries        = 5,
        CancellationToken cancellationToken = default)
    {
        var idInPayload = aggregateId.ToString("D");
        return await context.Outboxes
            .AsNoTracking()
            .AnyAsync(
                o => !o.Processed
                     && o.RetryCount < maxRetries
                     && o.Kind == kind
                     && o.Payload.Contains(idInPayload),
                cancellationToken);
    }
}
