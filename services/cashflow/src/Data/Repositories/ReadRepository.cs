using ArchChallenge.CashFlow.Data.Context;
using ArchChallenge.CashFlow.Data.Specifications;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Specifications;
using Microsoft.EntityFrameworkCore;

namespace ArchChallenge.CashFlow.Data.Repositories;

public class ReadRepository<T>(CashFlowDbContext context) : IReadRepository<T>
    where T : Entity
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        => await SpecificationEvaluator<T>.GetQuery(_dbSet.AsNoTracking(), spec)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? spec = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (spec is not null)
            query = SpecificationEvaluator<T>.GetQuery(query, spec);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<T>? spec = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (spec is not null)
            query = SpecificationEvaluator<T>.GetQuery(query, spec);

        return await query.CountAsync(cancellationToken);
    }
}
