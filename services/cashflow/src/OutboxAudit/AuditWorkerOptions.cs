namespace ArchChallenge.CashFlow.Infrastructure.Outbox.Audit;

public sealed class AuditWorkerOptions
{
    public const string SectionName = "AuditWorker";

    public int PollingIntervalSeconds { get; set; } = 3;

    public int BatchSize { get; set; } = 50;

    public int MaxRetries { get; set; } = 5;
}
