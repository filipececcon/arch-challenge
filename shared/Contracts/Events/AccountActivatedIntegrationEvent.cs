using System.Text.Json.Serialization;

namespace ArchChallenge.Contracts.Events;

/// <summary>Contrato de integração publicado em <c>cashflow.events</c> ao ativar uma conta corrente.</summary>
public sealed record AccountActivatedIntegrationEvent(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("eventName")] string EventName,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt,
    [property: JsonPropertyName("payload")] AccountActivatedPayload Payload);

public sealed record AccountActivatedPayload(
    [property: JsonPropertyName("accountId")] Guid AccountId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt);
