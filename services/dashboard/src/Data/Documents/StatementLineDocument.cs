using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ArchChallenge.Dashboard.Infrastructure.Data.Documents;

/// <summary>Linha de extrato: um documento por evento de transação processado.</summary>
public sealed class StatementLineDocument
{
    [BsonId]
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string UserId { get; set; } = "";

    /// <summary>Chave de dia UTC no formato yyyy-MM-dd — usada como índice de range.</summary>
    public string Day { get; set; } = "";

    public DateTime OccurredAt { get; set; }

    /// <summary>CREDIT ou DEBIT.</summary>
    public string Type { get; set; } = "";

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }
}
