using System.Text.Json;
using ArchChallenge.CashFlow.Domain.Shared.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Options;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Logging.Workers;
using ArchChallenge.CashFlow.Infrastructure.Data.Relational.Contexts;
using Microsoft.Extensions.Options;
using OutboxEntity = ArchChallenge.CashFlow.Domain.Shared.Entities.Outbox;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Workers;

/// <summary>
/// Processa registros de outbox com destino <see cref="OutboxTarget.Audit"/>
/// e persiste no immudb na tabela correspondente ao tipo de agregado.
/// </summary>
public sealed class AuditOutboxWorkerService(
    IServiceScopeFactory              scopeFactory,
    IAuditWriter                      writer,
    IOptions<AuditWorkerOptions>      options,
    IHostEnvironment                  hostEnvironment,
    ILogger<AuditOutboxWorkerService> logger)
    : OutboxWorkerBase(hostEnvironment, logger)
{
    private readonly AuditWorkerOptions _options = options.Value;

    protected override string WorkerName            => nameof(AuditOutboxWorkerService);
    protected override int    PollingIntervalSeconds => _options.PollingIntervalSeconds;
    protected override int    BatchSize              => _options.BatchSize;
    protected override int    MaxRetries             => _options.MaxRetries;
    protected override IServiceScopeFactory ScopeFactory => scopeFactory;

    public override async Task<IReadOnlyList<OutboxEntity>> FetchPendingAsync(
        IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        
        return await repo
            .GetPendingAsync(OutboxTarget.Audit, _options.BatchSize, _options.MaxRetries, cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task ProcessSingleAsync(OutboxEntity outbox, CancellationToken cancellationToken)
    {
        try
        {
            var entry = DeserializeEntry(outbox.Payload);

            await writer.WriteAuditEntryAsync(entry, cancellationToken).ConfigureAwait(false);

            outbox.MarkProcessed();

            logger.LogInformation(
                "[{WorkerName}] persisted to immudb — OutboxId={OutboxId}, AuditId={AuditId}, AggregateType={AggregateType}, AggregateId={AggregateId}, EventName={EventName}",
                WorkerName, outbox.Id, entry.AuditId, entry.AggregateType, entry.AggregateId, entry.EventName);
        }
        catch (Exception ex)
        {
            outbox.IncrementRetry();

            var level = outbox.RetryCount >= _options.MaxRetries ? LogLevel.Critical : LogLevel.Warning;

            logger.Log(level, ex,
                "[{WorkerName}] Failed to process OutboxId={OutboxId}. " +
                "Attempt {Retry}/{MaxRetries}.{Exhausted}",
                WorkerName, outbox.Id, outbox.RetryCount, _options.MaxRetries,
                outbox.RetryCount >= _options.MaxRetries
                    ? " MAX RETRIES REACHED — row will be excluded from future polling."
                    : string.Empty);
        }
    }

    public override async Task PersistChangesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        
        await repo.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Garante que o <c>FOR UPDATE SKIP LOCKED</c> permaneça ativo do fetch ao commit,
    /// prevenindo duplicatas entre réplicas concorrentes do worker de auditoria.
    /// </summary>
    protected override async Task ExecutePollInTransactionAsync(
        IServiceScope scope, Func<Task> work, CancellationToken cancellationToken)
    {
        var ctx = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
        
        await using var tx = await ctx.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            await work().ConfigureAwait(false);
            
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            
            throw;
        }
    }

    private static AuditEntry DeserializeEntry(string json)
    {
        using var doc = JsonDocument.Parse(json);
        
        var root      = doc.RootElement;

        return new AuditEntry(
            AuditId:       root.GetProperty("auditId").GetString()!,
            AggregateType: root.GetProperty("aggregateType").GetString()!,
            AggregateId:   root.GetProperty("aggregateId").GetString()!,
            EventName:     root.GetProperty("eventName").GetString()!,
            UserId:        root.GetProperty("userId").GetString()!,
            OccurredAt:    root.GetProperty("occurredAt").GetDateTime(),
            Payload:       root.TryGetProperty("state", out var state) ? state.GetRawText() : json);
    }
}
