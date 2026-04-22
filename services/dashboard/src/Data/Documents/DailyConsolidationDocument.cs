using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ArchChallenge.Dashboard.Infrastructure.Data.Documents;

/// <summary>Read model: totais agregados por conta e dia (UTC). <c>_id</c> = accountId|yyyy-MM-dd.</summary>
public sealed class DailyConsolidationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "";

    public Guid AccountId { get; set; }

    public string UserId { get; set; } = "";

    /// <summary>Dia UTC yyyy-MM-dd (redundante para filtros por intervalo).</summary>
    public string Day { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalCredits { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalDebits { get; set; }

    public DateTime UpdatedAt { get; set; }
}
