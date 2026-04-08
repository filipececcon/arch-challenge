using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Consumers;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ExecuteTransactionConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                // Exchange de entrada para criação de transações
                cfg.Message<CreateTransactionMessage>(m => m.SetEntityName("cashflow.transaction.create"));
                cfg.Publish<CreateTransactionMessage>(p => p.ExchangeType = ExchangeType.Direct);

                // Fila consumida pelo ExecuteTransactionConsumer
                cfg.ReceiveEndpoint("cashflow.transaction.create", e =>
                {
                    e.Bind("cashflow.transaction.create");
                    e.ConfigureConsumer<ExecuteTransactionConsumer>(ctx);
                });
                
                // Exchange de saída para downstream (dashboard, etc.)
                cfg.Message<TransactionDoneEvent>(m => m.SetEntityName("cashflow.transaction.done"));
                cfg.Publish<TransactionDoneEvent>(p => p.ExchangeType = ExchangeType.Topic);
            });
        });

        return services;
    }
}
