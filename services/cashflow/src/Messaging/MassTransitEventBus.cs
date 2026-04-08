using ArchChallenge.CashFlow.Application.Common.Interfaces;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging;

public sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
        => publishEndpoint.Publish(message, cancellationToken);
}
