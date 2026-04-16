using System.Text.Json;
using System.Text.Json.Serialization;
using ArchChallenge.CashFlow.Domain.Shared.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Events;

/// <summary>
/// Background service responsável por processar eventos pendentes do Outbox.
///
/// Fluxo de execução (a cada N segundos):
///   1. Lê até <see cref="OutboxWorkerOptions.BatchSize"/> OutboxEvents pendentes no PostgreSQL.
///   2. Para cada evento: delega a projeção para o MongoDB ao <see cref="IDocumentProjectionWriter"/>.
///   3. Marca o evento como processado (ou incrementa RetryCount em caso de falha).
///   4. Persiste o estado atualizado no PostgreSQL.
///
/// Garantias:
///   - Idempotência: upsert por ID garante que re-processamentos não duplicam dados.
///   - Resiliência: eventos com até <see cref="OutboxWorkerOptions.MaxRetries"/> falhas são descartados.
///   - Isolamento: usa escopo dedicado por ciclo para evitar DbContext compartilhado.
///   - Desacoplamento: não depende do MongoDB.Driver — usa apenas <see cref="IDocumentProjectionWriter"/>.
///
/// Referência: https://microservices.io/patterns/data/transactional-outbox.html
/// </summary>
public sealed class OutboxWorkerService(
    IServiceScopeFactory          scopeFactory,
    IDocumentProjectionWriter     projectionWriter,
    IOptions<OutboxWorkerOptions> options,
    IHostEnvironment              hostEnvironment,
    ILogger<OutboxWorkerService>  logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly OutboxWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[OutboxWorkerService] started — polling every {Interval}s, batch size {BatchSize}.",
            _options.PollingIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error in the OutboxWorker cycle.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("[OutboxWorkerService] stopped.");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outboxRepo        = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var pending = await outboxRepo.GetPendingAsync(_options.BatchSize, cancellationToken);

        if (hostEnvironment.IsEnvironment("Local"))
        {
            using (OutboxPollCycleLogging.BeginPollCycleScope())
            {
                logger.LogInformation(
                    "[OutboxWorker] poll cycle — pending {Count}: {Snapshot}",
                    pending.Count,
                    SerializeOutboxPendingSnapshot(pending));
            }
        }

        if (pending.Count == 0) return;

        foreach (var outboxEvent in pending)
            await ProcessSingleEventAsync(outboxEvent, cancellationToken);

        await outboxRepo.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSingleEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.CollectionMap.TryGetValue(outboxEvent.EventType, out var collectionName))
            {
                logger.LogWarning(
                    "[OutboxWorker] no collection mapped for EventType '{EventType}'. OutboxEvent {OutboxEventId} skipped.",
                    outboxEvent.EventType, outboxEvent.Id);
                outboxEvent.IncrementRetry();
                return;
            }

            await projectionWriter.UpsertAsync(collectionName, outboxEvent.Payload, cancellationToken);

            outboxEvent.MarkProcessed();

            logger.LogInformation(
                "[OutboxEvent] persisted to MongoDB — OutboxEventId={OutboxEventId}, EventType={EventType}, Collection={Collection}",
                outboxEvent.Id, outboxEvent.EventType, collectionName);
        }
        catch (Exception ex)
        {
            outboxEvent.IncrementRetry();

            logger.LogWarning(ex,
                "Failed to process OutboxEvent {OutboxEventId}. Attempt {Retry}/{MaxRetries}.",
                outboxEvent.Id, outboxEvent.RetryCount, _options.MaxRetries);
        }
    }

    private static string SerializeOutboxPendingSnapshot(IReadOnlyList<OutboxEvent> rows)
    {
        var snapshot = rows.Select(static r => new OutboxSnapshotRow(
            r.Id,
            r.EventType,
            r.CreatedAt,
            r.RetryCount,
            r.Processed,
            r.Payload));

        return JsonSerializer.Serialize(snapshot, SnapshotJsonOptions);
    }

    private sealed record OutboxSnapshotRow(
        Guid Id,
        string EventType,
        DateTime CreatedAt,
        int RetryCount,
        bool Processed,
        string Payload);
}
