using System.Text.Json;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Outbox.Audit;

/// <summary>
/// Processa <see cref="AuditEvent"/> e persiste no immudb na tabela correspondente ao tipo de agregado.
/// </summary>
public sealed class AuditOutboxWorkerService(
    IServiceScopeFactory              scopeFactory,
    IAuditWriter                      writer,
    IOptions<AuditWorkerOptions>      options,
    IHostEnvironment                  hostEnvironment,
    ILogger<AuditOutboxWorkerService> logger)
    : OutboxWorkerBase<AuditEvent>(hostEnvironment, logger)
{
    private readonly AuditWorkerOptions _options = options.Value;

    protected override string WorkerName            => nameof(AuditOutboxWorkerService);
    protected override int    PollingIntervalSeconds => _options.PollingIntervalSeconds;
    protected override int    BatchSize              => _options.BatchSize;
    protected override int    MaxRetries             => _options.MaxRetries;
    protected override IServiceScopeFactory ScopeFactory => scopeFactory;

    protected override async Task<IReadOnlyList<AuditEvent>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IAuditOutboxRepository>();
        return await repo.GetPendingAsync(_options.BatchSize, _options.MaxRetries, cancellationToken)
                         .ConfigureAwait(false);
    }

    protected override async Task ProcessSingleAsync(AuditEvent row, CancellationToken cancellationToken)
    {
        try
        {
            var entry = DeserializeEntry(row.Payload);

            await writer.WriteAuditEntryAsync(entry, cancellationToken).ConfigureAwait(false);

            row.MarkProcessed();

            logger.LogInformation(
                "[AuditOutbox] persisted to immudb — OutboxEventId={OutboxEventId}, AuditId={AuditId}, AggregateType={AggregateType}, AggregateId={AggregateId}, EventName={EventName}",
                row.Id, entry.AuditId, entry.AggregateType, entry.AggregateId, entry.EventName);
        }
        catch (Exception ex)
        {
            row.IncrementRetry();

            logger.LogWarning(ex,
                "Failed to process AuditEvent {Id}. Attempt {Retry}/{MaxRetries}.",
                row.Id, row.RetryCount, _options.MaxRetries);
        }
    }

    protected override async Task PersistChangesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IAuditOutboxRepository>();
        await repo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static AuditEntry DeserializeEntry(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;

        return new AuditEntry(
            AuditId:       root.GetProperty("auditId").GetString()!,
            AggregateType: root.GetProperty("aggregateType").GetString()!,
            AggregateId:   root.GetProperty("aggregateId").GetString()!,
            EventName:     root.GetProperty("eventName").GetString()!,
            UserId:        root.GetProperty("userId").GetString()!,
            OccurredAt:    root.GetProperty("occurredAt").GetDateTime(),
            Payload:       root.TryGetProperty("state", out var state) ? state.GetRawText() : json);
    }
}
