namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Registries;

/// <summary>
/// Implementação thread-safe do registro de coleções MongoDB.
/// Singleton preenchido no startup; falha explicitamente se um tipo não for registrado.
/// </summary>
internal sealed class CollectionNameRegistry : ICollectionNameRegistry
{
    private readonly ConcurrentDictionary<Type, string> _map = new();

    public void Register<TDocument>(string collectionName) where TDocument : class
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            throw new ArgumentException("Collection name cannot be empty.", nameof(collectionName));

        _map[typeof(TDocument)] = collectionName;
    }

    public string GetNameOrThrow<TDocument>() where TDocument : class
    {
        if (_map.TryGetValue(typeof(TDocument), out var name))
            return name;

        throw new InvalidOperationException(
            $"No MongoDB collection registered for type '{typeof(TDocument).FullName}'. " +
            "Register the collection in AddDocumentsData using the AddMongoCollections method.");
    }
}
