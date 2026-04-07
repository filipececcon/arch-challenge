namespace ArchChallenge.CashFlow.Infrastructure.Data.Repositories;

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
        CancellationToken cancellationToken = default)
        => await context.OutboxEvents
            .Where(o => !o.Processed && o.RetryCount < 5)
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}


