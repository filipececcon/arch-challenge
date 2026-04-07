using ArchChallenge.CashFlow.Data.Context;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ArchChallenge.CashFlow.Data.Repositories;

public class WriteRepository<T>(CashFlowDbContext context) : IWriteRepository<T>
    where T : Entity, IAggregateRoot
{
    private readonly DbSet<T> _dbSet = context.Set<T>();

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
