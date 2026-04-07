using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Workers;

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
    IServiceScopeFactory  scopeFactory,
    IMongoDatabase        mongoDatabase,
    ILogger<OutboxWorkerService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const           string   CollectionName  = "transactions_view";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxWorkerService iniciado — polling a cada {Interval}s.",
            PollingInterval.TotalSeconds);

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

            await Task.Delay(PollingInterval, stoppingToken);
        }

        logger.LogInformation("OutboxWorkerService encerrado.");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        // Cria um escopo dedicado para o DbContext (IOutboxRepository é Scoped)
        await using var scope      = scopeFactory.CreateAsyncScope();
        var outboxRepo             = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var pending                = await outboxRepo.GetPendingAsync(50, cancellationToken);

        if (pending.Count == 0) return;

        logger.LogDebug("OutboxWorker: {Count} evento(s) pendente(s) encontrado(s).", pending.Count);

        var collection = mongoDatabase.GetCollection<BsonDocument>(CollectionName);

        foreach (var outboxEvent in pending)
        {
            try
            {
                // Desserializa o payload JSON para BsonDocument
                var document = BsonDocument.Parse(outboxEvent.Payload);

                // Usa o ID da transação como _id do MongoDB para garantir idempotência
                if (document.TryGetValue("id", out var idValue))
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
                    "OutboxEvent {EventId} ({EventType}) sincronizado no MongoDB com sucesso.",
                    outboxEvent.Id, outboxEvent.EventType);
            }
            catch (Exception ex)
            {
                outboxEvent.IncrementRetry();

                logger.LogWarning(ex,
                    "Falha ao processar OutboxEvent {EventId}. Tentativa {Retry}/5.",
                    outboxEvent.Id, outboxEvent.RetryCount);
            }
        }

        // Persiste todos os markProcessed / incrementRetry de uma vez
        await outboxRepo.SaveChangesAsync(cancellationToken);
    }
}

