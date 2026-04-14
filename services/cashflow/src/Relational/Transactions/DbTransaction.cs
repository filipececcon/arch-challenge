using Microsoft.EntityFrameworkCore.Storage;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Transactions;

/// <summary>
/// Adapter que envolve o <see cref="IDbContextTransaction"/> do EF Core
/// expondo apenas a interface <see cref="IDbTransaction"/> do domínio,
/// mantendo a camada <c>Application</c> livre de dependências de infraestrutura.
/// </summary>
internal sealed class DbTransaction(IDbContextTransaction inner) : IDbTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
        => inner.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default)
        => inner.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => inner.DisposeAsync();
}
