using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.ReadModels;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Mappings;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Projections;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Serialization;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Documents;

public static class DependencyInjection
{
    /// <summary>
    /// Registra a infraestrutura MongoDB: cliente, banco, registry de coleções,
    /// resolver e repositórios genéricos de leitura.
    /// Os binds de tipo → coleção são declarados aqui mesmo; adicione novos
    /// documentos com <c>registry.Register&lt;TDocument&gt;("nome_da_colecao")</c>
    /// ao criar read models no projeto Documents.
    /// </summary>
    public static IServiceCollection AddDocumentsData(this IServiceCollection services, IConfiguration configuration)
    {
        MongoBsonGuidSetup.EnsureConfigured();
        TransactionDocumentClassMap.Register();

        var connectionString = configuration.GetConnectionString("MongoConnection");
        var databaseName     = configuration["MongoDB:Database"];

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        var registry = new CollectionNameRegistry();

        RegisterCollections(registry);

        services.AddSingleton<ICollectionNameRegistry>(registry);
        services.AddSingleton<IMongoCollectionResolver, MongoCollectionResolver>();

        services.AddScoped(typeof(IDocumentsReadRepository<>), typeof(DocumentsReadRepository<>));

        services.AddSingleton<IDocumentProjectionWriter, DocumentProjectionWriter>();

        return services;
    }

    /// <summary>
    /// Ponto único de declaração dos binds TDocument → coleção MongoDB.
    /// Adicione uma linha por documento ao criar novos read models.
    /// Exemplo: registry.Register&lt;TransactionDocument&gt;("transactions");
    /// </summary>
    private static void RegisterCollections(ICollectionNameRegistry registry)
    {
        registry.Register<TransactionDocument>("transactions");
    }
}
