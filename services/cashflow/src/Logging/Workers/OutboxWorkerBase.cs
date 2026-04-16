using System.Text.Json;
using System.Text.Json.Serialization;
using ArchChallenge.CashFlow.Domain.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;

/// <summary>
/// Template base para workers de outbox transacional.
///
/// Encapsula o loop de polling, tratamento de erros, log de snapshot em ambiente Local
/// e o fluxo de buscar → processar → persistir, deixando como pontos de extensão:
/// <list type="bullet">
///   <item><see cref="FetchPendingAsync"/> — busca eventos pendentes via repositório específico.</item>
///   <item><see cref="ProcessSingleAsync"/> — processa um evento (projeção ou auditoria).</item>
///   <item><see cref="PersistChangesAsync"/> — persiste as alterações de estado dos eventos.</item>
/// </list>
/// </summary>
/// <typeparam name="TEvent">Tipo concreto do evento de outbox (<see cref="EventBase"/>).</typeparam>
public abstract class OutboxWorkerBase<TEvent>(IHostEnvironment hostEnvironment, ILogger logger)
    : BackgroundService
    where TEvent : EventBase
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected abstract string WorkerName           { get; }
    protected abstract int    PollingIntervalSeconds { get; }
    protected abstract int    BatchSize             { get; }
    protected abstract int    MaxRetries            { get; }

    protected abstract Task<IReadOnlyList<TEvent>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken);

    protected abstract Task ProcessSingleAsync(
        TEvent evt, CancellationToken cancellationToken);

    protected abstract Task PersistChangesAsync(
        IServiceScope scope, CancellationToken cancellationToken);

    /// <summary>
    /// Fábrica de escopo injetada pela subclasse para criar escopos por ciclo de polling.
    /// </summary>
    protected abstract IServiceScopeFactory ScopeFactory { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "[{Worker}] started — polling every {Interval}s, batch size {BatchSize}.",
            WorkerName, PollingIntervalSeconds, BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unexpected error in the {Worker} cycle.", WorkerName);
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("[{Worker}] stopped.", WorkerName);
    }

    private async Task PollCycleAsync(CancellationToken cancellationToken)
    {
        await using var scope = ScopeFactory.CreateAsyncScope();

        var pending = await FetchPendingAsync(scope, cancellationToken);

        if (hostEnvironment.IsEnvironment("Local"))
        {
            using (OutboxPollCycleLogging.BeginPollCycleScope())
            {
                logger.LogInformation(
                    "[{Worker}] poll cycle — pending {Count}: {Snapshot}",
                    WorkerName,
                    pending.Count,
                    SerializePendingSnapshot(pending));
            }
        }

        if (pending.Count == 0) return;

        foreach (var evt in pending)
            await ProcessSingleAsync(evt, cancellationToken);

        await PersistChangesAsync(scope, cancellationToken);
    }

    private static string SerializePendingSnapshot(IReadOnlyList<TEvent> rows)
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
        Guid     Id,
        string   EventType,
        DateTime CreatedAt,
        int      RetryCount,
        bool     Processed,
        string   Payload);
}
