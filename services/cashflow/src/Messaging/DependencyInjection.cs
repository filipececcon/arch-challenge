using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Handlers;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.Message<TransactionRegisteredEvent>(m => m.SetEntityName("cashflow.transaction.done"));
                cfg.Publish<TransactionRegisteredEvent>(p => p.ExchangeType = ExchangeType.Direct);
            });
        });

        // Registra os INotificationHandler do Messaging no pipeline do MediatR.
        // Cada novo evento de domínio ganha um handler aqui sem alterar os command handlers.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TransactionRegisteredEventHandler).Assembly));

        return services;
    }
}
