namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Options;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";

    public int PollingIntervalSeconds { get; init; } = 3;

    public int BatchSize { get; init; } = 50;

    public int MaxRetries { get; init; } = 5;

    /// <summary>Kind (nome do evento) → nome da coleção MongoDB (projeção read model).</summary>
    public Dictionary<string, string> CollectionMap { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Kind → tipo CLR usado na deserialização e publicação no broker (outbox de Events).</summary>
    public Dictionary<string, Type> TypeMap { get; init; } = new(StringComparer.Ordinal);
}
