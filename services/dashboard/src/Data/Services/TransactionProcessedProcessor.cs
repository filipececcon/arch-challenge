using System.Data;
using ArchChallenge.Contracts.Events;
using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Data.Context;
using ArchChallenge.Dashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchChallenge.Dashboard.Data.Services;

public class TransactionProcessedProcessor(DashboardDbContext db) : ITransactionProcessedProcessor
{
    public async Task ProcessAsync(TransactionRegisteredIntegrationEvent message, CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var exists = await db.ProcessedIntegrationEvents
                .AsNoTracking()
                .AnyAsync(e => e.EventId == message.EventId, cancellationToken);

            if (exists)
            {
                await tx.CommitAsync(cancellationToken);
                return;
            }

            var occurredUtc = message.OccurredAt.Kind == DateTimeKind.Utc
                ? message.OccurredAt
                : message.OccurredAt.ToUniversalTime();

            var day = DateOnly.FromDateTime(occurredUtc);

            var consolidation = await db.DailyConsolidations
                .FirstOrDefaultAsync(c => c.Date == day, cancellationToken);

            if (consolidation is null)
            {
                consolidation = new DailyConsolidation
                {
                    Date = day,
                    TotalCredits = 0,
                    TotalDebits = 0,
                    UpdatedAt = DateTime.UtcNow
                };
                db.DailyConsolidations.Add(consolidation);
            }

            var amount = message.Payload.Amount;
            if (string.Equals(message.Payload.Type, "CREDIT", StringComparison.OrdinalIgnoreCase))
                consolidation.TotalCredits += amount;
            else if (string.Equals(message.Payload.Type, "DEBIT", StringComparison.OrdinalIgnoreCase))
                consolidation.TotalDebits += amount;

            consolidation.UpdatedAt = DateTime.UtcNow;

            db.ProcessedIntegrationEvents.Add(new ProcessedIntegrationEvent
            {
                EventId = message.EventId,
                ProcessedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }
}
