namespace ArchChallenge.Dashboard.Domain.Entities;

/// <summary>Garante idempotência no consumo (ADR-003).</summary>
public class ProcessedIntegrationEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; }
}
