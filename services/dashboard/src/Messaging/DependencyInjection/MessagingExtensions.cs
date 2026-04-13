using ArchChallenge.Dashboard.Messaging.Consumers;
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
            x.AddConsumer<TransactionProcessedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.ReceiveEndpoint("dashboard.transaction.processed", e =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.Bind("cashflow.events", b =>
                    {
                        b.ExchangeType = ExchangeType.Topic;
                        b.RoutingKey = "#";
                    });
                    e.ConfigureConsumer<TransactionProcessedConsumer>(context);
                });
            });
        });

        return services;
    }
}
