using ArchChallenge.CashFlow.Domain.Shared.Events;

namespace ArchChallenge.CashFlow.Domain.Shared.Interfaces;

public interface IAuditOutboxRepository
{
    Task<IReadOnlyList<AuditEvent>> GetPendingAsync(int batchSize= 50, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
