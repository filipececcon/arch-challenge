using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Events;

public static class DependencyInjection
{
    public static IServiceCollection AddEventsOutboxWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<OutboxWorkerOptions>()
            .BindConfiguration(OutboxWorkerOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OutboxWorkerOptions>, OutboxWorkerOptionsValidator>();

        services.AddHostedService<OutboxWorkerService>();

        return services;
    }
}
