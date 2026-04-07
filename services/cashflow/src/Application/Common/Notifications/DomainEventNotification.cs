namespace ArchChallenge.CashFlow.Application.Common.Notifications;

/// <summary>
/// Wrapper que adapta qualquer <see cref="IDomainEvent"/> para o sistema de
/// notificações do MediatR (<see cref="INotification"/>).
///
/// Mantém o domínio e o Shared livres de dependências externas: a ponte com
/// o MediatR existe somente na camada Application.
///
/// O <c>CommandHandlerBase</c> cria instâncias deste tipo via reflexão,
/// preservando o tipo concreto do evento para que o MediatR consiga rotear
/// para o <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c> correto.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent Event) : INotification
    where TEvent : IDomainEvent;
