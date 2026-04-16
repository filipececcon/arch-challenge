using ArchChallenge.CashFlow.Domain.Shared.Events;

namespace ArchChallenge.CashFlow.Domain.Shared.Audit;

/// <summary>
/// DTO que representa uma entrada de auditoria pronta para gravação no banco imutável via SQL.
/// Desserializado a partir do payload do <see cref="AuditEvent"/>.
/// </summary>
public sealed record AuditEntry(
    string   AuditId,
    string   AggregateType,
    string   AggregateId,
    string   EventName,
    string   UserId,
    DateTime OccurredAt,
    string   Payload);
