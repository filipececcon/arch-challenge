namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

public interface IWriteRepository<in T> where T : Entity
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
