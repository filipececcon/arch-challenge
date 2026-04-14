using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Serialization;

/// <summary>
/// O driver MongoDB 3.x exige representação BSON explícita para <see cref="Guid"/>.
/// Sem isso, filtros LINQ com <c>Guid</c> lançam
/// <see cref="BsonSerializationException"/> (GuidRepresentation Unspecified).
/// </summary>
internal static class MongoBsonGuidSetup
{
    private static readonly object Gate = new();
    private static bool _done;

    public static void EnsureConfigured()
    {
        if (_done) return;
        lock (Gate)
        {
            if (_done) return;

#pragma warning disable CS0618 // GuidRepresentation — necessário no driver 3.x para serializar Guid em filtros
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
#pragma warning restore CS0618

            _done = true;
        }
    }
}
