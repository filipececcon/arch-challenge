using MongoDB.Bson.Serialization;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Mappings;

internal static class TransactionDocumentClassMap
{
    private static readonly object Gate = new();
    private static bool _done;

    public static void Register()
    {
        if (_done) return;
        
        lock (Gate)
        {
            if (_done) return;

            if (!BsonClassMap.IsClassMapRegistered(typeof(TransactionDocument)))
            {
                BsonClassMap.RegisterClassMap<TransactionDocument>(cm =>
                {
                    cm.MapIdProperty(d => d.Id);
                    cm.MapProperty(d => d.Type).SetElementName("type");
                    cm.MapProperty(d => d.Amount).SetElementName("amount");
                    cm.MapProperty(d => d.Description).SetElementName("description");
                    cm.MapProperty(d => d.CreatedAt).SetElementName("createdAt");
                    cm.MapProperty(d => d.UpdatedAt).SetElementName("updatedAt");
                    cm.MapProperty(d => d.Active).SetElementName("active");
                    cm.SetIgnoreExtraElements(true);
                });
            }

            _done = true;
        }
    }
}
