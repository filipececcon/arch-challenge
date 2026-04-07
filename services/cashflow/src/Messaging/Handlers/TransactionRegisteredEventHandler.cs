namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Handlers;

/// <summary>
/// Notification handler responsável por publicar o evento
/// <see cref="TransactionRegisteredEvent"/> no RabbitMQ via MassTransit.
///
/// É ativado automaticamente pelo MediatR quando o <c>CommandHandlerBase</c>
/// despacha o <see cref="DomainEventNotification{TEvent}"/> ao final da execução.
/// Novos side-effects podem ser adicionados criando novos handlers para o mesmo
/// tipo de notificação sem alterar nenhum command handler existente.
/// </summary>
public class TransactionRegisteredEventHandler(
    IPublishEndpoint publishEndpoint,
    ILogger<TransactionRegisteredEventHandler> logger)
    : INotificationHandler<DomainEventNotification<TransactionRegisteredEvent>>
{
    public async Task Handle(
        DomainEventNotification<TransactionRegisteredEvent> notification,
        CancellationToken cancellationToken)
    {
        var @event = notification.Event;

        await publishEndpoint.Publish(@event, ctx
            => ctx.SetRoutingKey("cashflow.transaction.done"), cancellationToken);

        logger.LogInformation(
            "Event {EventType} published for transaction {EventId}",
            @event.EventType,
            @event.EventId);
    }
}
