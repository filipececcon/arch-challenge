using System.Reflection;
using ArchChallenge.CashFlow.Application.Abstractions.Messaging;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Attributes;
using Microsoft.Extensions.Configuration;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;

public static class DependencyInjection
{
    /// <summary>
    /// Configura apenas o publisher MassTransit (IEventBus) sem registrar consumers nem canais.
    /// Use em workers de outbox que só precisam publicar eventos, sem consumir.
    /// </summary>
    public static IServiceCollection AddMessagingPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((_, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.Publish<INotification>(p => p.Exclude = true);
            });
        });

        return services;
    }

    /// <summary>
    /// Configura o bus completo: publisher + consumers + canais.
    /// Use na API / host que processa mensagens (ex.: ExecuteTransactionConsumer).
    /// </summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IEventBus, MassTransitEventBus>();

        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMassTransit(x =>
        {
            RegisterConsumers(x, assembly);

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMQ:Host"], "/", h =>
                {
                    h.Username(configuration["RabbitMQ:Username"]!);
                    h.Password(configuration["RabbitMQ:Password"]!);
                });

                cfg.Publish<INotification>(p => p.Exclude = true);

                foreach (var channel in DiscoverChannels(assembly))
                    channel.Configure(cfg);

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }

    private static void RegisterConsumers(IBusRegistrationConfigurator configurator, Assembly assembly)
    {
        var consumerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.GetInterfaces().Any(i => i.IsGenericType
                                                      && i.GetGenericTypeDefinition() == typeof(IConsumer<>)));

        foreach (var consumerType in consumerTypes)
        {
            var hasChannelAttribute = consumerType.GetCustomAttributes(inherit: false)
                .OfType<IConsumerEndpointMetadata>()
                .Any();

            var definitionType = hasChannelAttribute
                ? typeof(AttributeConsumerDefinition<>).MakeGenericType(consumerType)
                : null;

            configurator.AddConsumer(consumerType, definitionType);
        }
    }

    private static IEnumerable<IChannel> DiscoverChannels(Assembly assembly)
        => assembly.GetTypes()
            .Where(t => typeof(IChannel).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .Cast<IChannel>();
}
