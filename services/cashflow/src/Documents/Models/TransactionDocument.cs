namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;

/// <summary>
/// Documento de leitura alinhado ao JSON gravado no Mongo pelo outbox
/// (camelCase, <c>type</c> como string). Não usar a entidade de domínio <c>Transaction</c>:
/// setters privados e enum não deserializam com o driver Mongo.
/// </summary>
public sealed class TransactionDocument
{
    public Guid     Id          { get; set; }
    public string   Type        { get; set; } = null!;
    public decimal  Amount      { get; set; }
    public string?  Description { get; set; }
    public DateTime CreatedAt   { get; set; }
    public DateTime UpdatedAt   { get; set; }
    public bool     Active      { get; set; }
}
