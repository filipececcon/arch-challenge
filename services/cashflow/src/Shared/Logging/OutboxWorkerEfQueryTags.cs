namespace ArchChallenge.CashFlow.Domain.Shared.Logging;

/// <summary>
/// Marcadores usados em <c>TagWith</c> nas consultas EF do outbox worker.
/// O projeto de logging filtra esses comandos para não inundar sinks (ex.: Elasticsearch).
/// </summary>
public static class OutboxWorkerEfQueryTags
{
    /// <summary>
    /// Deve coincidir com o argumento de <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.TagWith"/> no repositório.
    /// </summary>
    public const string PendingBatchQueryMarker = "OutboxWorker: pending batch";
}
