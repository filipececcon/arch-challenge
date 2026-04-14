using System.Text.Json;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;
using ArchChallenge.CashFlow.Domain.Shared.Projection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Outbox;

/// <summary>
/// Background service responsável por processar eventos pendentes do Outbox.
///
/// Fluxo de execução (a cada 5 segundos):
///   1. Lê até 50 OutboxEvents com ST_PROCESSED = false no PostgreSQL
///   2. Para cada evento: o payload JSON é gravado na coleção mapeada (read model).
///      Remove-se campos só de runtime (Flunt / base <c>Entity</c>):
///      <c>isValid</c>, <c>isFailure</c>, <c>notifications</c>. O documento fica com <c>_id</c>
///      e os dados persistidos da entidade.
///   3. Marca o evento como processado (ou incrementa RetryCount em caso de falha)
///   4. Persiste o estado atualizado no PostgreSQL
///
/// Garantias:
///   - Idempotência: upsert por ID garante que re-processamentos não duplicam dados
///   - Resiliência: eventos com até 5 falhas são descartados automaticamente
///   - Isolamento: usa escopo dedicado por ciclo para evitar DbContext compartilhado
///
/// Referência: https://microservices.io/patterns/data/transactional-outbox.html
/// </summary>
public sealed class OutboxWorkerService(
    IServiceScopeFactory          scopeFactory,
    IMongoDatabase                mongoDatabase,
    IOptions<OutboxWorkerOptions> options,
    ILogger<OutboxWorkerService>  logger) : BackgroundService
{
    private readonly OutboxWorkerOptions _options = options.Value;

    // -------------------------------------------------------------------------
    // BackgroundService
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // Orquestração do ciclo de polling
    // -------------------------------------------------------------------------

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        // Cria um escopo dedicado para o DbContext (IOutboxRepository é Scoped)
        await using var scope   = scopeFactory.CreateAsyncScope();
        var outboxRepo          = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var pending= await outboxRepo.GetPendingAsync(_options.BatchSize, cancellationToken);

        if (pending.Count == 0) return;

        logger.LogDebug("[OutboxWorker] {Count} pending event(s) found.", pending.Count);

        foreach (var outboxEvent in pending)
            await ProcessSingleEventAsync(outboxEvent, cancellationToken);

        // Persiste todos os MarkProcessed / IncrementRetry de uma vez
        await outboxRepo.SaveChangesAsync(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Processamento de um único evento
    // -------------------------------------------------------------------------

    private async Task ProcessSingleEventAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.CollectionMap.TryGetValue(outboxEvent.EventType, out var collectionName))
            {
                logger.LogWarning(
                    "[OutboxWorker] no collection mapped for EventName '{EventName}'. Event {OutboxEventId} skipped.",
                    outboxEvent.EventType, outboxEvent.Id);
                outboxEvent.IncrementRetry();
                return;
            }

            var collection = mongoDatabase.GetCollection<BsonDocument>(collectionName);
            var document   = BuildProjectionDocument(outboxEvent);

            var filter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
            await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);

            outboxEvent.MarkProcessed();

            logger.LogInformation(
                "[OutboxEvent] {OutboxEventId} ({EventName}) successfully synced to MongoDB.",
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

    // -------------------------------------------------------------------------
    // Construção do documento MongoDB a partir do payload
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converte o payload JSON do <paramref name="outboxEvent"/> em um <see cref="BsonDocument"/>
    /// pronto para upsert no MongoDB:
    /// <list type="bullet">
    ///   <item>Define <c>_id</c> a partir do campo <c>id</c>/<c>Id</c> do payload (ou do outboxEvent como fallback).</item>
    ///   <item>Remove o campo <c>id</c>/<c>Id</c> para evitar duplicidade com <c>_id</c>.</item>
    ///   <item>Remove campos de runtime herdados de Flunt / base <c>Entity</c>.</item>
    /// </list>
    /// </summary>
    private static BsonDocument BuildProjectionDocument(OutboxEvent outboxEvent)
    {
        var element = JsonSerializer.Deserialize<JsonElement>(outboxEvent.Payload)!;
        element = EntityProjectionJson.RemoveRuntimeFields(element);
        var document = BsonDocument.Parse(element.GetRawText());

        // Após normalização camelCase, o id costuma vir como "id".
        if (document.TryGetValue("Id", out var idValue) || document.TryGetValue("id", out idValue))
            document["_id"] = idValue;
        else if (!document.Contains("_id"))
            document["_id"] = outboxEvent.Id.ToString();

        document.Remove("id");
        document.Remove("Id");

        return document;
    }
}
