using System.Text.Json;
using System.Text.Json.Serialization;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
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
///   <item><see cref="FetchPendingAsync"/> — busca registros pendentes via repositório específico.</item>
///   <item><see cref="ProcessSingleAsync"/> — processa um registro (projeção ou auditoria).</item>
///   <item><see cref="PersistChangesAsync"/> — persiste as alterações de estado.</item>
/// </list>
/// </summary>
public abstract class OutboxWorkerBase(IHostEnvironment hostEnvironment, ILogger logger)
    : BackgroundService, IOutboxWorker
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented          = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    protected abstract string WorkerName             { get; }
    protected abstract int    PollingIntervalSeconds { get; }
    protected abstract int    BatchSize              { get; }
    protected abstract int    MaxRetries             { get; }

    /// <summary>Fábrica de escopo injetada pela subclasse para criar escopos por ciclo de polling.</summary>
    protected abstract IServiceScopeFactory ScopeFactory { get; }

    public abstract Task<IReadOnlyList<Outbox>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken);

    public abstract Task ProcessSingleAsync(
        Outbox outbox, CancellationToken cancellationToken);

    public abstract Task PersistChangesAsync(
        IServiceScope scope, CancellationToken cancellationToken);

    /// <summary>
    /// Cada worker de outbox roda o loop de polling em uma <see cref="Thread"/> com
    /// <see cref="Thread.Name"/> = <see cref="WorkerName"/>, evitando contenção com o
    /// pool padrão e isolar os três destinos (Mongo, Events, Audit) em fios distintos.
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var completion = new TaskCompletionSource();
        var thread = new Thread(() => RunDedicatedWorkerLoop(stoppingToken, completion))
        {
            IsBackground = true,
            Name         = WorkerName
        };
        thread.Start();
        return completion.Task;
    }

    private void RunDedicatedWorkerLoop(
        CancellationToken     stoppingToken,
        TaskCompletionSource  completion)
    {
        try
        {
            RunDedicatedWorkerAsync(stoppingToken).GetAwaiter().GetResult();
            completion.TrySetResult();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            completion.TrySetResult();
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    private async Task RunDedicatedWorkerAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "[{Worker}] started — dedicated thread, polling every {Interval}s, batch size {BatchSize}.",
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

        await ExecutePollInTransactionAsync(scope, async () =>
        {
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

            foreach (var outbox in pending)
                await ProcessSingleAsync(outbox, cancellationToken);

            await PersistChangesAsync(scope, cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Envolve o ciclo de polling (fetch → process → persist) em um contexto transacional.
    /// A implementação padrão não abre transação — subclasses devem sobrescrever para
    /// ativar semântica de <c>FOR UPDATE SKIP LOCKED</c> quando múltiplas réplicas forem
    /// implantadas, abrindo e commitando uma transação no <c>DbContext</c> antes e depois do work.
    /// </summary>
    protected virtual Task ExecutePollInTransactionAsync(
        IServiceScope     scope,
        Func<Task>        work,
        CancellationToken cancellationToken)
        => work();

    private static string SerializePendingSnapshot(IReadOnlyList<Outbox> rows)
    {
        var snapshot = rows.Select(static r => new OutboxSnapshotRow(
            r.Id,
            r.Kind,
            r.Target,
            r.CreatedAt,
            r.RetryCount,
            r.Processed,
            r.Payload));

        return JsonSerializer.Serialize(snapshot, SnapshotJsonOptions);
    }

    private sealed record OutboxSnapshotRow(
        Guid         Id,
        string       Kind,
        OutboxTarget Target,
        DateTime     CreatedAt,
        int          RetryCount,
        bool         Processed,
        string       Payload);
}
