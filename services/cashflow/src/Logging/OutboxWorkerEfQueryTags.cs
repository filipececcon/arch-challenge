namespace ArchChallenge.CashFlow.Domain.Shared.Logging;

/// <summary>
/// Marcadores usados em <c>TagWith</c> nas consultas EF dos workers de outbox.
/// O projeto de logging filtra esses comandos SQL para não inundar sinks (ex.: Elasticsearch).
/// Cada constante deve coincidir com o argumento de <c>TagWith</c> no repositório correspondente.
/// </summary>
public static class OutboxWorkerEfQueryTags
{
    public const string PendingBatchQueryMarker      = "OutboxWorker: pending batch";
    public const string AuditPendingBatchQueryMarker = "AuditOutboxWorker: pending batch";
}
