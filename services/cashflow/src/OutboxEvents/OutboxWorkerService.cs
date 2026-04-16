using ArchChallenge.CashFlow.Domain.Shared.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    ILogger<OutboxWorkerService>  logger)
    : OutboxWorkerBase<OutboxEvent>(hostEnvironment, logger)
{
    private readonly OutboxWorkerOptions _options = options.Value;

    protected override string WorkerName            => nameof(OutboxWorkerService);
    protected override int    PollingIntervalSeconds => _options.PollingIntervalSeconds;
    protected override int    BatchSize              => _options.BatchSize;
    protected override int    MaxRetries             => _options.MaxRetries;
    protected override IServiceScopeFactory ScopeFactory => scopeFactory;

    protected override async Task<IReadOnlyList<OutboxEvent>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        return await repo.GetPendingAsync(_options.BatchSize, _options.MaxRetries, cancellationToken);
    }

    protected override async Task ProcessSingleAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
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

    protected override async Task PersistChangesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await repo.SaveChangesAsync(cancellationToken);
    }
}
