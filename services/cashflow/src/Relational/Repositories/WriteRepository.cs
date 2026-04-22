using ArchChallenge.CashFlow.Domain.Shared.Specifications;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational.Specifications;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Repositories;

public class WriteRepository<T>(CashFlowDbContext context) : IWriteRepository<T>
    where T : Entity
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        => await SpecificationEvaluator<T>.GetQuery(_dbSet.AsTracking(), spec)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
