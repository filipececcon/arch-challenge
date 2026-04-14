using System.Text.Json;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Projection;
using MongoDB.Bson;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Projections;

/// <summary>
/// Implementação de <see cref="IDocumentProjectionWriter"/> que:
/// <list type="bullet">
///   <item>Remove campos de runtime do payload JSON (Flunt / base Entity).</item>
///   <item>Normaliza nomes de propriedades para camelCase.</item>
///   <item>Define <c>_id</c> a partir do campo <c>id</c>/<c>Id</c> do payload.</item>
///   <item>Faz upsert atômico na coleção MongoDB indicada.</item>
/// </list>
/// </summary>
internal sealed class DocumentProjectionWriter(IMongoDatabase database) : IDocumentProjectionWriter
{
    public async Task UpsertAsync(
        string            collectionName,
        string            jsonPayload,
        CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<BsonDocument>(collectionName);
        var document   = BuildDocument(jsonPayload);

        var filter = Builders<BsonDocument>.Filter.Eq("_id", document["_id"]);
        await collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, cancellationToken);
    }

    /// <summary>
    /// Converte o payload JSON em <see cref="BsonDocument"/> pronto para upsert:
    /// remove campos de runtime, normaliza camelCase e promove <c>id</c>/<c>Id</c> para <c>_id</c>.
    /// </summary>
    private static BsonDocument BuildDocument(string jsonPayload)
    {
        var element  = JsonSerializer.Deserialize<JsonElement>(jsonPayload);
        element      = EntityProjectionJson.RemoveRuntimeFields(element);
        var document = BsonDocument.Parse(element.GetRawText());

        if (document.TryGetValue("Id", out var idValue) || document.TryGetValue("id", out idValue))
            document["_id"] = NormalizeIdForStorage(idValue);
        else if (!document.Contains("_id"))
            document["_id"] = BsonValue.Create(Guid.NewGuid().ToString());

        document.Remove("id");
        document.Remove("Id");

        return document;
    }

    /// <summary>
    /// Grava <c>_id</c> como UUID BinData (Standard) quando o payload traz Guid em string,
    /// alinhando com a leitura via <see cref="Guid"/> e com <c>FindOneByIdAsync</c>.
    /// </summary>
    private static BsonValue NormalizeIdForStorage(BsonValue idValue)
    {
        if (idValue.BsonType == BsonType.String && Guid.TryParse(idValue.AsString, out var guid))
        {
            #pragma warning disable CS0618
            
            return new BsonBinaryData(guid, GuidRepresentation.Standard);
            
            #pragma warning restore CS0618
        }

        return idValue;
    }
}
