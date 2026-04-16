namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Events;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";
    public int PollingIntervalSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 50;
    public int MaxRetries { get; init; } = 5;

    /// <summary>
    /// Mapeia EventType → nome da coleção MongoDB.
    /// Exemplo: { "TransactionProcessed": "transactions" }
    /// </summary>
    public Dictionary<string, string> CollectionMap { get; init; } = [];
}
