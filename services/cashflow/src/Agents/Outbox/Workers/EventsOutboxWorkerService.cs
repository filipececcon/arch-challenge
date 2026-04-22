using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Options;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational.Contexts;
using Microsoft.Extensions.Options;
using OutboxEntity = ArchChallenge.CashFlow.Domain.Shared.Entities.Outbox;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Workers;

/// <summary>
/// Background service responsável por processar registros de outbox com destino <see cref="OutboxTarget.Events"/>.
///
/// Fluxo de execução (a cada N segundos):
///   1. Lê até <see cref="OutboxWorkerOptions.BatchSize"/> registros pendentes (FOR UPDATE SKIP LOCKED).
///   2. Para cada registro: desserializa o payload para o CLR type mapeado em <see cref="OutboxWorkerOptions.TypeMap"/>
///      e publica no broker via <see cref="IEventBus"/>.
///   3. Marca o registro como processado (ou incrementa RetryCount em caso de falha).
///   4. Persiste e commita a transação.
///
/// O mapeamento Kind → Type é configurado programaticamente no startup (não via appsettings).
/// </summary>
public sealed class EventsOutboxWorkerService(
    IServiceScopeFactory                  scopeFactory,
    IOptions<OutboxWorkerOptions>         options,
    IHostEnvironment                      hostEnvironment,
    ILogger<EventsOutboxWorkerService> logger)
    : OutboxWorkerBase(hostEnvironment, logger)
{
    private readonly OutboxWorkerOptions _options = options.Value;

    protected override string WorkerName             => nameof(EventsOutboxWorkerService);
    protected override int    PollingIntervalSeconds => _options.PollingIntervalSeconds;
    protected override int    BatchSize              => _options.BatchSize;
    protected override int    MaxRetries             => _options.MaxRetries;
    protected override IServiceScopeFactory ScopeFactory => scopeFactory;

    public override async Task<IReadOnlyList<OutboxEntity>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        return await repo.GetPendingAsync(OutboxTarget.Events, _options.BatchSize, _options.MaxRetries, cancellationToken);
    }

    public override async Task ProcessSingleAsync(OutboxEntity outbox, CancellationToken cancellationToken)
    {
        try
        {
            if (!_options.TypeMap.TryGetValue(outbox.Kind, out var eventType))
            {
                outbox.MarkProcessed();
                logger.LogCritical(
                    "[{WorkerName}] POISON MESSAGE — Kind '{Kind}' has no type mapping. " +
                    "OutboxId={OutboxId} permanently discarded. Register '{Kind}' in OutboxWorkerOptions.TypeMap.",
                    WorkerName ,outbox.Kind, outbox.Id, outbox.Kind);
                return;
            }

            var message = JsonSerializer.Deserialize(outbox.Payload, eventType);
            if (message is null)
            {
                outbox.MarkProcessed();
                logger.LogCritical(
                    "[{WorkerName}] Deserialization returned null — Kind={Kind}, OutboxId={OutboxId}. Discarded.",
                    WorkerName, outbox.Kind, outbox.Id);
                return;
            }

            await using var publishScope = scopeFactory.CreateAsyncScope();
            var eventBus = publishScope.ServiceProvider.GetRequiredService<IEventBus>();
            await eventBus.PublishAsync(message, eventType, cancellationToken);

            outbox.MarkProcessed();

            logger.LogInformation(
                "[{WorkerName}] published to broker — OutboxId={OutboxId}, Kind={Kind}, Type={Type}",
                WorkerName, outbox.Id, outbox.Kind, eventType.Name);
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
