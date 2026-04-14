namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents.Registries;

/// <summary>
/// Registro de mapeamento Tipo C# → nome de coleção MongoDB.
/// Preenchido uma única vez no startup via <see cref="DependencyInjection.AddDocumentsData"/>.
/// </summary>
public interface ICollectionNameRegistry
{
    /// <summary>Registra o par <typeparamref name="TDocument"/> → <paramref name="collectionName"/>.</summary>
    void Register<TDocument>(string collectionName) where TDocument : class;

    /// <summary>
    /// Retorna o nome de coleção para <typeparamref name="TDocument"/>.
    /// Lança <see cref="InvalidOperationException"/> se o tipo não estiver registrado.
    /// </summary>
    string GetNameOrThrow<TDocument>() where TDocument : class;
}
