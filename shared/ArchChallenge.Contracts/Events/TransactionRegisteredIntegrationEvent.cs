using System.Text.Json.Serialization;

namespace ArchChallenge.Contracts.Events;

/// <summary>Contrato de integração publicado em <c>cashflow.events</c> (ver ADR-007).</summary>
public sealed record TransactionRegisteredIntegrationEvent(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt,
    [property: JsonPropertyName("payload")] TransactionRegisteredPayload Payload);

public sealed record TransactionRegisteredPayload(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("amount")] decimal Amount);
