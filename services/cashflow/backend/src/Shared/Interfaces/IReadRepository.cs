using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Specifications;

namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

public interface IReadRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T>? spec = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(ISpecification<T>? spec = null, CancellationToken cancellationToken = default);
}
