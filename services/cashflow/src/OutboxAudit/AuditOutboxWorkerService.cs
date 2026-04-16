using System.Text.Json;
using System.Text.Json.Serialization;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
using Microsoft.Extensions.Hosting;
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
    ILogger<AuditOutboxWorkerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AuditWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "[AuditOutboxWorker] started — polling every {Interval}s, batch size {BatchSize}.",
            _options.PollingIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error in the AuditOutboxWorker cycle.");
            }

            await Task
                .Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken)
                .ConfigureAwait(false);
        }

        logger.LogInformation("[AuditOutboxWorker] stopped.");
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var repo = scope.ServiceProvider.GetRequiredService<IAuditOutboxRepository>();

        var pending = await repo.GetPendingAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);

        if (hostEnvironment.IsEnvironment("Local"))
        {
            using (OutboxPollCycleLogging.BeginPollCycleScope())
            {
                logger.LogInformation(
                    "[AuditOutboxWorker] poll cycle — pending {Count}: {Snapshot}",
                    pending.Count,
                    SerializeAuditPendingSnapshot(pending));
            }
        }

        if (pending.Count == 0) return;

        foreach (var row in pending)
        {
            await ProcessSingleAsync(row, cancellationToken).ConfigureAwait(false);
        }

        await repo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessSingleAsync(AuditEvent row, CancellationToken cancellationToken)
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

    private static string SerializeAuditPendingSnapshot(IReadOnlyList<AuditEvent> rows)
    {
        var snapshot = rows.Select(static r => new AuditOutboxSnapshotRow(
            r.Id,
            r.EventType,
            r.CreatedAt,
            r.RetryCount,
            r.Processed,
            r.Payload));

        return JsonSerializer.Serialize(snapshot, SnapshotJsonOptions);
    }

    private sealed record AuditOutboxSnapshotRow(
        Guid Id,
        string EventType,
        DateTime CreatedAt,
        int RetryCount,
        bool Processed,
        string Payload);

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
