namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;

public interface IWriteRepository<T> where T : Entity, IAggregateRoot
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
