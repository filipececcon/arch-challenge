using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Registries;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Resolvers;

internal sealed class MongoCollectionResolver(
    IMongoDatabase           database,
    ICollectionNameRegistry  registry) : IMongoCollectionResolver
{
    public IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class
    {
        var name = registry.GetNameOrThrow<TDocument>();
        return database.GetCollection<TDocument>(name);
    }
}
