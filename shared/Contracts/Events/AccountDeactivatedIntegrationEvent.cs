using System.Text.Json.Serialization;

namespace ArchChallenge.Contracts.Events;

/// <summary>Contrato de integração publicado em <c>cashflow.events</c> ao desativar uma conta corrente.</summary>
public sealed record AccountDeactivatedIntegrationEvent(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("eventName")] string EventName,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt,
    [property: JsonPropertyName("payload")] AccountDeactivatedPayload Payload);

public sealed record AccountDeactivatedPayload(
    [property: JsonPropertyName("accountId")] Guid AccountId,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("updatedAt")] DateTime UpdatedAt);
