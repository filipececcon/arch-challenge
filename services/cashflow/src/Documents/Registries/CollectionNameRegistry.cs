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
            throw new ArgumentException("O nome da coleção não pode ser vazio.", nameof(collectionName));

        _map[typeof(TDocument)] = collectionName;
    }

    public string GetNameOrThrow<TDocument>() where TDocument : class
    {
        if (_map.TryGetValue(typeof(TDocument), out var name))
            return name;

        throw new InvalidOperationException(
            $"Nenhuma coleção MongoDB registrada para o tipo '{typeof(TDocument).FullName}'. " +
            "Registre a coleção em AddDocumentsData usando o método AddMongoCollections.");
    }
}
