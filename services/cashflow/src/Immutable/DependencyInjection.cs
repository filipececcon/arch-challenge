namespace ArchChallenge.CashFlow.Infrastructure.Data.Immutable;

public static class DependencyInjection
{
    public static IServiceCollection AddImmutableData(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ImmuDbOptions>(configuration.GetSection(ImmuDbOptions.SectionName));

        services.AddSingleton<IAuditWriter, AuditWriter>();

        services.AddSingleton<ImmuDbHealthCheck>();

        return services;
    }

    public static IHealthChecksBuilder AddImmuDbHealthCheck(this IHealthChecksBuilder builder)
        => builder.AddCheck<ImmuDbHealthCheck>("immudb", tags: ["ready"]);
}
