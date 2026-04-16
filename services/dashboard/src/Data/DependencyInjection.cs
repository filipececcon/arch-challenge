using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Data.Services;
using ArchChallenge.Dashboard.Data.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Data;

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
        services.AddScoped<ITransactionProcessedProcessor, TransactionProcessedProcessor>();

        return services;
    }
}
