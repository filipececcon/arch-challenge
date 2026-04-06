using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

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

        var message = new
        {
            EventId = @event.EventId,
            EventType = @event.EventType,
            OccurredAt = @event.OccurredAt,
            Payload = new
            {
                TransactionId = @event.TransactionId,
                Type = @event.Type.ToString().ToUpperInvariant(),
                Amount = @event.Amount,
                Description = @event.Description
            }
        };

        await publishEndpoint.Publish(message, cancellationToken);

        logger.LogInformation(
            "Event {EventType} published for transaction {TransactionId}",
            @event.EventType,
            @event.TransactionId);
    }
}
