using ArchChallenge.CashFlow.Data.Context;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;

namespace ArchChallenge.CashFlow.Data;

public sealed class UnitOfWork(CashFlowDbContext context) : IUnitOfWork
{
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new DbTransaction(transaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
