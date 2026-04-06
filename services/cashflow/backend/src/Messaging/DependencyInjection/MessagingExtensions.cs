using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Handlers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.DependencyInjection;

public static class MessagingExtensions
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

                cfg.Message<object>(m => m.SetEntityName("cashflow.events"));
            });
        });

        // Registra os INotificationHandler do Messaging no pipeline do MediatR.
        // Cada novo evento de domínio ganha um handler aqui sem alterar os command handlers.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(TransactionRegisteredEventHandler).Assembly));

        return services;
    }
}
