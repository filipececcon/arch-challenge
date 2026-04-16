using ArchChallenge.Contracts.Events;
using ArchChallenge.Dashboard.Infrastructure.Data;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace ArchChallenge.Dashboard.Infrastructure.CrossCutting.Messaging.Consumers;

public class TransactionProcessedConsumer(IServiceScopeFactory scopeFactory)
    : IConsumer<TransactionRegisteredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionRegisteredIntegrationEvent> context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<ITransactionProcessedProcessor>();
        await processor.ProcessAsync(context.Message, context.CancellationToken);
    }
}
