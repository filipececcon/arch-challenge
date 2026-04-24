using ArchChallenge.CashFlow.Application.Abstractions.Messaging;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;

public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        => publishEndpoint.Publish(message, cancellationToken);

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
        => publishEndpoint.Publish(message, messageType, cancellationToken);
}
