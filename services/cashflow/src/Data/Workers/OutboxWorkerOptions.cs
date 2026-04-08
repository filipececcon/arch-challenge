namespace ArchChallenge.CashFlow.Infrastructure.Data.Workers;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";
    public int PollingIntervalSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 50;
    public int MaxRetries { get; init; } = 5;
}
