using ArchChallenge.CashFlow.Infrastructure.Data.Repositories;
using ArchChallenge.CashFlow.Infrastructure.Data.Transactions;
using ArchChallenge.CashFlow.Infrastructure.Data.Workers;

namespace ArchChallenge.CashFlow.Infrastructure.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CashFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IOutboxRepository, OutboxRepository>();

        var mongoConnectionString = configuration.GetConnectionString("MongoConnection");
        var mongoDatabaseName     = configuration["MongoDB:Database"];
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

        services.AddHostedService<OutboxWorkerService>();

        return services;
    }

    public static async Task MigrateAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CashFlowDbContext>();
        await db.Database.MigrateAsync();
    }
}
