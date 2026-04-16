namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Caching;

public static class DependencyInjection
{
    public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("RedisConnection"));

        services.AddScoped<ITaskCacheService, TaskCacheService>();

        return services;
    }
}
