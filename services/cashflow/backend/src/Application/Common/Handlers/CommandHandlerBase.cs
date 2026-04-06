using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Application.Common.Responses;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using MediatR;

namespace ArchChallenge.CashFlow.Application.Common.Handlers;

/// <summary>
/// Template Method base para todos os command handlers.
///
/// O método <see cref="Handle"/> orquestra as etapas dentro de uma transação de banco de dados:
///   1. <see cref="BeforeExecuteAsync"/> — preparação (idempotência, validações de estado).
///   2. <see cref="ExecuteAsync"/>       — lógica de domínio (fase Prepare do 2PC).
///   3. [automático] <see cref="IUnitOfWork.SaveChangesAsync"/> — persiste todas as alterações.
///   4. <see cref="AfterExecuteAsync"/>  — pós-processamento e cleanup.
///   5. [automático] Commit da transação — torna as alterações permanentes.
///   6. [automático] cada evento coletado via <see cref="RaiseEvent"/> é despachado pelo MediatR
///      somente após o commit, garantindo consistência entre persistência e mensageria.
///
/// Em caso de qualquer exceção nas etapas 1–4, o Rollback é executado automaticamente
/// e a exceção é relançada. Os eventos pendentes são descartados.
///
/// O parâmetro genérico <typeparamref name="TEvent"/> define o tipo concreto do evento
/// de domínio emitido por este handler, eliminando a necessidade de reflexão para criar
/// o wrapper <see cref="DomainEventNotification{TEvent}"/>.
/// </summary>
public abstract class CommandHandlerBase<TCommand, TResponse, TEvent>(
    IPublisher publisher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<TCommand, TResponse>
    where TCommand : IRequest<TResponse>
    where TEvent : IDomainEvent
    where TResponse : Result
{
    private readonly List<TEvent> _pendingEvents = [];

    /// <summary>
    /// Enfileira um evento de domínio tipado para despacho via MediatR após o commit.
    /// Deve ser chamado dentro de <see cref="ExecuteAsync"/> após a criação/alteração do agregado.
    /// </summary>
    protected void RaiseEvent(TEvent domainEvent) => _pendingEvents.Add(domainEvent);

    // Não sobrescreva este método nas subclasses — estenda o comportamento via
    // BeforeExecuteAsync, ExecuteAsync e AfterExecuteAsync.
    public async Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await ExecuteAsync(command, cancellationToken);

            if(!response.IsValid) return response;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            foreach (var @event in _pendingEvents)
                await publisher.Publish(new DomainEventNotification<TEvent>(@event), cancellationToken);

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _pendingEvents.Clear();
        }
    }

    /// <summary>
    /// Etapa de execução principal: lógica de domínio.
    /// Chame <see cref="RaiseEvent"/> aqui para registrar eventos a serem despachados após o commit.
    /// Não é necessário chamar SaveChanges — a base executa automaticamente após este método.
    /// Corresponde à fase Prepare do protocolo 2PC.
    /// </summary>
    protected abstract Task<TResponse> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
}
