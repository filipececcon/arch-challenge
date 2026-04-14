using ArchChallenge.CashFlow.Application.Common.Interfaces;

namespace ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;

public sealed class TransactionProcessedHandler(IEventBus eventBus)
    : INotificationHandler<TransactionProcessedEvent>
{
    public Task Handle(TransactionProcessedEvent notification, CancellationToken cancellationToken)
        => eventBus.PublishAsync(notification.Payload, cancellationToken);
}
