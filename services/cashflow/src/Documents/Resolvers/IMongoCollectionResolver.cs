namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Resolvers;

/// <summary>
/// Resolve <see cref="IMongoCollection{TDocument}"/> a partir do tipo C# do documento,
/// consultando o <see cref="ICollectionNameRegistry"/> registrado no DI.
/// </summary>
public interface IMongoCollectionResolver
{
    IMongoCollection<TDocument> GetCollection<TDocument>() where TDocument : class;
}
