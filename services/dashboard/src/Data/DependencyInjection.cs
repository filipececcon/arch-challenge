using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Infrastructure.Data.Serialization;
using ArchChallenge.Dashboard.Infrastructure.Data.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Infrastructure.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        MongoBsonGuidSetup.EnsureConfigured();

        var connectionString = configuration.GetConnectionString("MongoConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:MongoConnection não configurada.");
        var databaseName = configuration["MongoDB:Database"]
            ?? throw new InvalidOperationException("MongoDB:Database não configurado.");

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));

        services.AddScoped<IDailyBalanceReadStore, DailyBalanceReadStore>();
        services.AddScoped<IStatementReadStore, StatementReadStore>();
        services.AddScoped<ITransactionProcessedProcessor, TransactionProcessedProcessor>();

        return services;
    }
}
