using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ArchChallenge.Dashboard.Infrastructure.Data.Documents;

/// <summary>Read model: totais agregados por dia (UTC), chave <c>_id</c> = ISO yyyy-MM-dd.</summary>
public sealed class DailyConsolidationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalCredits { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalDebits { get; set; }

    public DateTime UpdatedAt { get; set; }
}
