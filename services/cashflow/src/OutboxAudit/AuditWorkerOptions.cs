namespace ArchChallenge.CashFlow.Infrastructure.Outbox.Audit;

public sealed class AuditWorkerOptions
{
    public const string SectionName = "AuditWorker";

    public int PollingIntervalSeconds { get; init; } = 3;

    public int BatchSize { get; init; } = 50;

    public int MaxRetries { get; init; } = 5;
}
