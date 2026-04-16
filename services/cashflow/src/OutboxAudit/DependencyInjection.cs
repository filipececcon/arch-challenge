namespace ArchChallenge.CashFlow.Infrastructure.Outbox.Audit;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditOutboxWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AuditWorkerOptions>()
            .BindConfiguration(AuditWorkerOptions.SectionName)
            .ValidateOnStart();

        services.AddHostedService<AuditOutboxWorkerService>();

        return services;
    }
}
