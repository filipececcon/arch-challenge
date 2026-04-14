using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational.Outbox;

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
    IDocumentProjectionWriter        projectionWriter,
    IOptions<OutboxWorkerOptions> options,
    ILogger<OutboxWorkerService>  logger) : BackgroundService
{
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
        await using var scope  = scopeFactory.CreateAsyncScope();
        var outboxRepo         = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var pending            = await outboxRepo.GetPendingAsync(_options.BatchSize, cancellationToken);

        if (pending.Count == 0) return;

        logger.LogDebug("[OutboxWorker] {Count} pending event(s) found.", pending.Count);

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
                    "[OutboxWorker] no collection mapped for EventType '{EventType}'. Event {OutboxEventId} skipped.",
                    outboxEvent.EventType, outboxEvent.Id);
                outboxEvent.IncrementRetry();
                return;
            }

            await projectionWriter.UpsertAsync(collectionName, outboxEvent.Payload, cancellationToken);

            outboxEvent.MarkProcessed();

            logger.LogInformation(
                "[OutboxEvent] {OutboxEventId} ({EventType}) successfully synced to MongoDB.",
                outboxEvent.Id, outboxEvent.EventType);
        }
        catch (Exception ex)
        {
            outboxEvent.IncrementRetry();

            logger.LogWarning(ex,
                "Failed to process OutboxEvent {OutboxEventId}. Attempt {Retry}/{MaxRetries}.",
                outboxEvent.Id, outboxEvent.RetryCount, _options.MaxRetries);
        }
    }
}
