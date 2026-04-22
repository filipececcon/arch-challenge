namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

public interface IWriteRepository<T> where T : Entity
{
    /// <summary>
    /// Carrega a primeira entidade que satisfaz <paramref name="spec"/> com rastreamento de mudanças ativo,
    /// permitindo que alterações sejam detectadas pelo <c>SaveChanges</c> da UoW.
    /// Use dentro de uma transação quando precisar de exclusividade sobre o registro.
    /// </summary>
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
