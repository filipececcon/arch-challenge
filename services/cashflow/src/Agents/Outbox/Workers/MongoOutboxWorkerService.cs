using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Options;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational.Contexts;
using Microsoft.Extensions.Options;
using OutboxEntity = ArchChallenge.CashFlow.Domain.Shared.Entities.Outbox;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Workers;

/// <summary>
/// Background service responsável por processar registros de outbox com destino <see cref="OutboxTarget.Mongo"/>.
///
/// Fluxo de execução (a cada N segundos):
///   1. Lê até <see cref="OutboxWorkerOptions.BatchSize"/> registros pendentes no PostgreSQL (FOR UPDATE SKIP LOCKED).
///   2. Para cada registro: delega a projeção para o MongoDB ao <see cref="IDocumentProjectionWriter"/>.
///   3. Marca o registro como processado (ou incrementa RetryCount em caso de falha).
///   4. Persiste o estado atualizado no PostgreSQL e commita a transação.
///
/// Referências: https://microservices.io/patterns/data/transactional-outbox.html
/// </summary>
public sealed class MongoOutboxWorkerService(
    IServiceScopeFactory          scopeFactory,
    IDocumentProjectionWriter     projectionWriter,
    IOptions<OutboxWorkerOptions> options,
    IHostEnvironment              hostEnvironment,
    ILogger<MongoOutboxWorkerService>  logger)
    : OutboxWorkerBase(hostEnvironment, logger)
{
    private readonly OutboxWorkerOptions _options = options.Value;

    protected override string WorkerName             => nameof(MongoOutboxWorkerService);
    protected override int    PollingIntervalSeconds => _options.PollingIntervalSeconds;
    protected override int    BatchSize              => _options.BatchSize;
    protected override int    MaxRetries             => _options.MaxRetries;
    protected override IServiceScopeFactory ScopeFactory => scopeFactory;

    public override async Task<IReadOnlyList<OutboxEntity>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        return await repo.GetPendingAsync(OutboxTarget.Mongo, _options.BatchSize, _options.MaxRetries, cancellationToken);
    }

    public override async Task ProcessSingleAsync(OutboxEntity outbox, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.CollectionMap.TryGetValue(outbox.Kind, out var collectionName))
            {
                outbox.MarkProcessed();

                logger.LogCritical(
                    "[{WorkerName}] POISON MESSAGE — Kind '{Kind}' has no collection mapping. " +
                    "OutboxId={OutboxId} permanently discarded. Add '{Kind}' to CollectionMap or investigate.",
                    WorkerName, outbox.Kind, outbox.Id, outbox.Kind);
                return;
            }

            await projectionWriter.UpsertAsync(collectionName, outbox.Payload, cancellationToken);

            outbox.MarkProcessed();

            logger.LogInformation(
                "[{WorkerName}] persisted to MongoDB — OutboxId={OutboxId}, Kind={Kind}, Collection={Collection}",
                WorkerName, outbox.Id, outbox.Kind, collectionName);
        }
        catch (Exception ex)
        {
            outbox.IncrementRetry();

            var level = outbox.RetryCount >= _options.MaxRetries
                ? LogLevel.Critical
                : LogLevel.Warning;

            logger.Log(level, ex,
                "[{WorkerName}] Failed to process OutboxId={OutboxId} (Kind={Kind}). " +
                "Attempt {Retry}/{MaxRetries}.{Exhausted}",
                WorkerName, outbox.Id, outbox.Kind, outbox.RetryCount, _options.MaxRetries,
                outbox.RetryCount >= _options.MaxRetries
                    ? " MAX RETRIES REACHED — row will be excluded from future polling."
                    : string.Empty);
        }
    }

    public override async Task PersistChangesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        await repo.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Abre uma transação no <see cref="CashFlowDbContext"/> compartilhado do escopo de polling,
    /// garantindo que o <c>FOR UPDATE SKIP LOCKED</c> de <see cref="IOutboxRepository.GetPendingAsync"/>
    /// mantenha o lock até o COMMIT ao final de PersistChangesAsync.
    /// </summary>
    protected override async Task ExecutePollInTransactionAsync(
        IServiceScope scope, Func<Task> work, CancellationToken cancellationToken)
    {
        var ctx = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
        await using var tx = await ctx.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await work();
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
