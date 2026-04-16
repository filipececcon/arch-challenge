using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace ArchChallenge.Dashboard.Data.Serialization;

/// <summary>
/// Representação BSON explícita para <see cref="Guid"/> em filtros e documentos.
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

#pragma warning disable CS0618
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
#pragma warning restore CS0618

            _done = true;
        }
    }
}
