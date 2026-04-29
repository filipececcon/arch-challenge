using System.Text.Json.Serialization;

namespace ArchChallenge.Contracts.Events;

/// <summary>Contrato de integração publicado em <c>cashflow.events</c> ao criar uma conta corrente.</summary>
public sealed record AccountCreatedIntegrationEvent(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("eventName")] string EventName,
    [property: JsonPropertyName("occurredAt")] DateTime OccurredAt,
    [property: JsonPropertyName("payload")] AccountCreatedPayload Payload);

public sealed record AccountCreatedPayload(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt);
