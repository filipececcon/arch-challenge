namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Transactions;

public sealed class UnitOfWork(CashFlowDbContext context) : IUnitOfWork
{
    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new DbTransaction(transaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);
}
