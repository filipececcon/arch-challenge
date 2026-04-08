namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Contrato que representa a unidade de trabalho responsável por controlar
/// o ciclo de vida de uma transação e persistir as alterações pendentes.
///
/// Desacoplado do repositório genérico, permitindo que o <c>CommandHandlerBase</c>
/// gerencie commit e rollback de forma centralizada sem precisar conhecer
/// o tipo concreto do agregado manipulado por cada handler filho.
/// </summary>
public interface IUnitOfWork
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
