using ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Outbox;

/// <summary>
/// Background service responsável por processar eventos pendentes do Outbox.
///
/// Fluxo de execução (a cada 5 segundos):
///   1. Lê até 50 OutboxEvents com ST_PROCESSED = false no PostgreSQL
///   2. Para cada evento: serializa o payload e faz upsert na coleção
///      <c>transactions_view</c> do MongoDB (read model / projeção CQRS)
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
    IServiceScopeFactory         scopeFactory,
    IMongoDatabase               mongoDatabase,
    IOptions<OutboxWorkerOptions> options,
    ILogger<OutboxWorkerService> logger) : BackgroundService
{
    private readonly OutboxWorkerOptions _options = options.Value;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[OutboxWorkerService] iniciado — polling a cada {Interval}s, batch {BatchSize}.",
            _options.PollingIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro inesperado no ciclo do OutboxWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("[OutboxWorkerService] encerrado.");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        // Cria um escopo dedicado para o DbContext (IOutboxRepository é Scoped)
        await using var scope = scopeFactory.CreateAsyncScope();
        
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        
        var pending = await outboxRepo.GetPendingAsync(_options.BatchSize, cancellationToken);

        if (pending.Count == 0) return;

        logger.LogDebug("[OutboxWorker] {Count} evento(s) pendente(s) encontrado(s).", pending.Count);

        foreach (var outboxEvent in pending)
        {
            try
            {
                if (!_options.CollectionMap.TryGetValue(outboxEvent.EventType, out var collectionName))
                {
                    logger.LogWarning(
                        "[OutboxWorker]: nenhuma coleção mapeada para EventName '{EventName}'. Evento {OutboxEventId} ignorado.",
                        outboxEvent.EventType, outboxEvent.Id);
                    outboxEvent.IncrementRetry();
                    continue;
                }

                var collection = mongoDatabase.GetCollection<BsonDocument>(collectionName);
                
                // Desserializa o payload JSON para BsonDocument
                var document = BsonDocument.Parse(outboxEvent.Payload);

                // Usa o ID da transação como _id do MongoDB para garantir idempotência
                if (document.TryGetValue("Id", out var idValue))
                    document["_id"] = idValue;

                // Metadados do evento para rastreabilidade
                document["_eventType"]  = outboxEvent.EventType;
                document["_occurredAt"] = outboxEvent.CreatedAt.ToString("o");

                // Upsert: garante que re-processamentos não duplicam documentos
                var filter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
                await collection.ReplaceOneAsync(
                    filter,
                    document,
                    new ReplaceOptions { IsUpsert = true },
                    cancellationToken);

                outboxEvent.MarkProcessed();

                logger.LogInformation(
                    "[OutboxEvent] {OutboxEventId} ({EventName}) sincronizado no MongoDB com sucesso.",
                    outboxEvent.Id, outboxEvent.EventType);
            }
            catch (Exception ex)
            {
                outboxEvent.IncrementRetry();

                logger.LogWarning(ex,
                    "Falha ao processar OutboxEvent {OutboxEventId}. Tentativa {Retry}/{MaxRetries}.",
                    outboxEvent.Id, outboxEvent.RetryCount, _options.MaxRetries);
            }
        }

        // Persiste todos os markProcessed / incrementRetry de uma vez
        await outboxRepo.SaveChangesAsync(cancellationToken);
    }
}

