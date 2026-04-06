namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

/// <summary>
/// Representa uma transação de banco de dados em andamento.
///
/// Mantém o domínio livre de dependências de frameworks de persistência:
/// a implementação concreta fica na camada <c>Data</c> via <c>EfTransaction</c>.
///
/// O <c>CommandHandlerBase</c> usa este contrato para garantir que toda
/// a unidade de trabalho de um command seja atômica:
///   - <see cref="CommitAsync"/> é chamado somente após SaveChanges e AfterExecute bem-sucedidos.
///   - <see cref="RollbackAsync"/> é chamado automaticamente em caso de qualquer exceção.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
