using System.Reflection;
using System.Text.Json;
using ArchChallenge.CashFlow.Application.Utils;

namespace ArchChallenge.CashFlow.Application.Common.Commands;

/// <summary>
/// Template Method para handlers que executam um comando de escrita.
/// Encapsula o fluxo de infraestrutura (transação, auditoria, task cache, ação pós-commit),
/// deixando <see cref="ExecuteAsync"/> como ponto de extensão para a regra de negócio.
/// </summary>
/// <typeparam name="TCommand">
/// Command de execução — deve herdar <see cref="CommandBase"/> e implementar <see cref="IAsyncCommand"/>.
/// </typeparam>
/// <typeparam name="TAggregate">Raiz de agregação produzida pelo handler filho.</typeparam>
/// <typeparam name="TMessage">Mensagem a ser enviada para o broker</typeparam>
public abstract class CommandHandlerBase<TCommand, TAggregate, TMessage>(
    IUnitOfWork                unitOfWork,
    IOutboxRepository          outboxRepository,
    IAuditContext              auditContext,
    ITaskCacheService          taskCache,
    IEventBus                  eventBus,
    IStringLocalizer<Messages> localizer)
    : IRequestHandler<TCommand>
    where TCommand   : CommandBase, IRequest, IAsyncCommand
    where TAggregate : Entity, IAggregateRoot
    where TMessage : MessageBase
{
    // Lido uma única vez por instanciação do tipo genérico (custo zero em runtime).
    // Convenção: toda TMessage deve declarar `public new const string EventName`.
    private static readonly string EventName =
        typeof(TMessage)
            .GetField("EventName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            ?.GetRawConstantValue() as string
        ?? typeof(TMessage).Name;
    
    /// <summary>
    /// Implementa a regra de negócio: cria a raiz de agregação, persiste no repositório
    /// </summary>
    protected abstract Task<TAggregate> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
    
    public async Task Handle(TCommand command, CancellationToken cancellationToken)
    {
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = await ExecuteAsync(command, cancellationToken);

            await WriteOutboxEvent(entity, cancellationToken);
            
            var result = GetCommandResult(entity);

            if (entity.IsFailure)
            {
                await taskCache.SetFailureAsync(command.TaskId, localizer[MessageKeys.Exception.DomainError], cancellationToken);
                await tx.RollbackAsync(cancellationToken);
                return;
            }

            auditContext.Capture(entity, EventName);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            await tx.CommitAsync(cancellationToken);

            await taskCache.SetSuccessAsync(command.TaskId, result.Payload, cancellationToken);

            if (result.AfterCommit is not null) await result.AfterCommit(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            await taskCache.SetFailureAsync(command.TaskId, localizer[MessageKeys.Exception.InternalError], cancellationToken);
            throw;
        }
    }
    
    private async Task WriteOutboxEvent(IAggregateRoot entity, CancellationToken cancellationToken)
    {
        // Usar o tipo em tempo de execução: serializar como IAggregateRoot (marker interface)
        // produz JSON vazio — o contrato vem só do tipo declarado em Serialize<T>.
        var json = JsonSerializer.Serialize(entity, entity.GetType(), SerializeUtils.EntityJsonOptions);

        var outboxEvent = new OutboxEvent(EventName, json);
        
        await outboxRepository.AddAsync(outboxEvent, cancellationToken);
    }

    private CommandResult<TAggregate> GetCommandResult(TAggregate entity)
    {
        var json    = JsonSerializer.SerializeToElement(entity, SerializeUtils.EntityJsonOptions);
        
        var message = (TMessage)Activator.CreateInstance(typeof(TMessage), json.GetRawText())!;

        return new CommandResult<TAggregate>(
            Aggregate:   entity,
            EventName:   EventName,
            Payload:     json,
            AfterCommit: ct => eventBus.PublishAsync(message, ct));
    }
}
