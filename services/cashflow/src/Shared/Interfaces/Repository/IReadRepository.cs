namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;

public interface IReadRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? spec = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T>? spec = null, CancellationToken cancellationToken = default);
}
