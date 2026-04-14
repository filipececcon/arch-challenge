using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Data.Relational;

public static class DependencyInjection
{
    public static IServiceCollection AddRelationalData(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CashFlowDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        services.AddOptions<OutboxWorkerOptions>()
            .BindConfiguration(OutboxWorkerOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OutboxWorkerOptions>, OutboxWorkerOptionsValidator>();

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
