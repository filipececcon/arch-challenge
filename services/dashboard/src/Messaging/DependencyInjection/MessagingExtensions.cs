using ArchChallenge.Dashboard.Messaging.Consumers;
using ArchChallenge.Contracts.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace ArchChallenge.Dashboard.Messaging.DependencyInjection;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TransactionRegisteredConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.Message<TransactionRegisteredIntegrationEvent>(m => m.SetEntityName("cashflow.events"));
                cfg.Publish<TransactionRegisteredIntegrationEvent>(p => p.ExchangeType = ExchangeType.Topic);

                cfg.ReceiveEndpoint("dashboard.lancamento.registrado", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind("cashflow.events", b =>
                    {
                        b.ExchangeType = ExchangeType.Topic;
                        b.RoutingKey = "lancamento.registrado";
                    });
                    e.ConfigureConsumer<TransactionRegisteredConsumer>(context);
                });
            });
        });

        return services;
    }
}
