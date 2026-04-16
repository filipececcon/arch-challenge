using MongoDB.Bson.Serialization.Attributes;

namespace ArchChallenge.Dashboard.Data.Documents;

/// <summary>Registro idempotente: uma linha por <see cref="Id"/> (= EventId do contrato de integração).</summary>
public sealed class ProcessedIntegrationEventDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public DateTime ProcessedAt { get; set; }
}
