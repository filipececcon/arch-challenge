using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Common.Handlers;

/// <summary>
/// Template Method base para todos os command handlers.
///
/// O método <see cref="Handle"/> orquestra as três etapas na ordem definida
/// e, ao final, despacha via MediatR todos os eventos levantados com
/// <see cref="RaiseEvent"/>, ativando qualquer <c>INotificationHandler</c>
/// registrado — incluindo a publicação no RabbitMQ.
///
///   1. <see cref="BeforeExecuteAsync"/> — preparação (idempotência, validações de estado).
///   2. <see cref="ExecuteAsync"/>       — lógica de domínio + persistência (fase Prepare do 2PC).
///   3. <see cref="AfterExecuteAsync"/>  — pós-processamento e cleanup (fase Commit local).
///   4. [automático] cada evento coletado é embrulhado em
///      <see cref="DomainEventNotification{TEvent}"/> e despachado pelo MediatR,
///      preservando o tipo concreto para roteamento correto dos handlers.
///
/// Todos os métodos de template são abstratos, forçando cada handler filho a
/// declarar explicitamente o comportamento das três etapas.
/// </summary>
public abstract class CommandHandlerBase<TCommand, TResponse>(IPublisher publisher)
    : IRequestHandler<TCommand, TResponse>
    where TCommand : IRequest<TResponse>
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    /// <summary>
    /// Enfileira um evento de domínio para despacho via MediatR ao final do <see cref="Handle"/>.
    /// Deve ser chamado dentro de <see cref="ExecuteAsync"/> após a criação/alteração do agregado.
    /// </summary>
    protected void RaiseEvent(IDomainEvent domainEvent) => _pendingEvents.Add(domainEvent);

    // Não sobrescreva este método nas subclasses — estenda o comportamento via
    // BeforeExecuteAsync, ExecuteAsync e AfterExecuteAsync.
    public async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await BeforeExecuteAsync(command, cancellationToken);
        var response = await ExecuteAsync(command, cancellationToken);
        await AfterExecuteAsync(command, response, cancellationToken);

        foreach (var @event in _pendingEvents)
            await publisher.Publish(WrapAsNotification(@event), cancellationToken);

        _pendingEvents.Clear();

        return response;
    }

    /// <summary>
    /// Etapa de pré-execução: preparação, checagens e inicialização de contexto
    /// que devem ocorrer antes da persistência.
    /// </summary>
    protected abstract Task BeforeExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Etapa de execução principal: lógica de domínio e persistência no banco de dados.
    /// Chame <see cref="RaiseEvent"/> aqui para registrar eventos a serem despachados.
    /// Corresponde à fase Prepare do protocolo 2PC.
    /// </summary>
    protected abstract Task<TResponse> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Etapa de pós-execução: cleanup e finalização após a persistência.
    /// O despacho dos eventos é gerenciado automaticamente pela base via MediatR.
    /// Corresponde à fase Commit local do protocolo 2PC.
    /// </summary>
    protected abstract Task AfterExecuteAsync(TCommand command, TResponse response, CancellationToken cancellationToken);

    /// <summary>
    /// Cria um <see cref="DomainEventNotification{TEvent}"/> com o tipo concreto do evento,
    /// preservando a informação de tipo para que o MediatR roteie para o handler correto.
    /// Ex.: <c>TransactionRegisteredEvent</c> →
    ///      <c>DomainEventNotification&lt;TransactionRegisteredEvent&gt;</c>.
    /// </summary>
    private static INotification WrapAsNotification(IDomainEvent domainEvent)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        return (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
    }
}
