using ArchChallenge.CashFlow.Application.Transactions.Execute;
using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Options;
using ArchChallenge.CashFlow.Infrastructure.Agents.Outbox.Workers;
using ArchChallenge.Contracts.Events;
using Microsoft.Extensions.Options;

namespace ArchChallenge.CashFlow.Infrastructure.Agents.Outbox;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os três workers de outbox (Mongo, Events, Audit), cada um com fila e thread dedicada.
    /// </summary>
    public static IServiceCollection AddOutboxAgent(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OutboxWorkerOptions>()
            .BindConfiguration(OutboxWorkerOptions.SectionName)
            .Configure(o =>
            {
                o.TypeMap[ExecuteTransactionCommandHandler.EventName] = typeof(TransactionRegisteredIntegrationEvent);
            })
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<OutboxWorkerOptions>, OutboxWorkerOptionsValidator>();

        services
            .AddOptions<AuditWorkerOptions>()
            .BindConfiguration(AuditWorkerOptions.SectionName)
            .ValidateOnStart();

        services.AddHostedService<MongoOutboxWorkerService>();
        services.AddHostedService<EventsOutboxWorkerService>();
        services.AddHostedService<AuditOutboxWorkerService>();

        return services;
    }
}
