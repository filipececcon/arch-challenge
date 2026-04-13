using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Domain.Events;

namespace ArchChallenge.CashFlow.Application.Transactions.Events;

public sealed class TransactionProcessedEventHandler(IEventBus eventBus)
    : INotificationHandler<DomainEventNotification<TransactionProcessedEvent>>
{
    public Task Handle(DomainEventNotification<TransactionProcessedEvent> notification, CancellationToken cancellationToken)
        => eventBus.PublishAsync(notification.Event, cancellationToken);
}
