using ArchChallenge.CashFlow.Domain.Shared.Audit;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Transactions;

public sealed class UnitOfWork(CashFlowDbContext context, IAuditContext auditContext) : IUnitOfWork
{
    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new DbTransaction(transaction);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Persiste a entidade principal primeiro: garante que IDs gerados pelo banco
        // (ou pelo EF tracking) já estejam preenchidos antes de tirar o snapshot de auditoria.
        var n = await context.SaveChangesAsync(cancellationToken);

        // O snapshot é tirado aqui, com o estado pós-persistência.
        if (auditContext.TryBuildAuditOutboxPayload(out var eventName, out var payload))
        {
            await context.AuditOutboxEvents.AddAsync(new AuditEvent(eventName, payload), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        auditContext.NotifyPersisted();
        return n;
    }
}
