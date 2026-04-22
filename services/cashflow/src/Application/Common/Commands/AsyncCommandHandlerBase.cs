using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Outbox;
using ArchChallenge.CashFlow.Application.Utils;
using Flunt.Notifications;

namespace ArchChallenge.CashFlow.Application.Common.Commands;

/// <summary>
/// Template Method para handlers que executam um comando de escrita assíncrona.
/// 
/// Parâmetros genéricos:
/// <list type="bullet">
///   <item><typeparamref name="TCommand"/>    — command; herda <see cref="CommandBase"/>, implementa <c>IRequest</c> e <see cref="IAsyncCommand"/>.</item>
///   <item><typeparamref name="TAggregate"/>  — raiz de agregação que contém a lógica de domínio e o <c>IsFailure</c>.</item>
///   <item><typeparamref name="TProjection"/> — entidade projetada no MongoDB e devolvida ao cliente no task-cache.
///         Em geral igual a <typeparamref name="TAggregate"/>; difere quando o handler opera em dois agregados
///         (ex.: <c>ExecuteTransactionHandler</c>: <c>TAggregate=Account</c>, <c>TProjection=Transaction</c>).</item>
/// </list>
/// 
/// Três entradas de outbox são gravadas na mesma transação (atomicamente) via
/// <see>
///     <cref>IOutboxMapperOutboxMapper{TCommand,TAggregate,TProjection}</cref>
/// </see>
/// :
/// <list type="bullet">
///   <item><see cref="OutboxTarget.Mongo"/>  — JSON de <typeparamref name="TProjection"/> no read model (MongoDB).</item>
///   <item><see cref="OutboxTarget.Audit"/>  — auditoria imutável (immudb), quando o mapeador retorna não-nulo.</item>
///   <item><see cref="OutboxTarget.Events"/> — evento de integração para o broker, quando o mapeador retorna não-nulo.</item>
/// </list>
/// </summary>
public abstract class AsyncCommandHandlerBase<TCommand, TAggregate, TProjection>(
    IUnitOfWork unitOfWork,
    IOutboxRepository outboxRepository,
    ITaskCacheService taskCache,
    IStringLocalizer<Messages> localizer,
    IOutboxMapper<TCommand, TAggregate, TProjection> mapper,
    OutboxWriter<TCommand, TAggregate, TProjection> outboxWriter)
    : Notifiable<Notification>, IRequestHandler<TCommand>
    where TCommand    : CommandBase, IRequest, IAsyncCommand
    where TAggregate  : Entity, IAggregateRoot
    where TProjection : Entity
{
    /// <summary>Regra de negócio: cria/atualiza o agregado e persiste no repositório.</summary>
    protected abstract Task<TAggregate?> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Extrai a entidade projetada (Mongo + cache) a partir do agregado retornado por <see cref="ExecuteAsync"/>.
    /// Padrão: cast direto — funciona quando <typeparamref name="TAggregate"/> == <typeparamref name="TProjection"/>.
    /// Sobrescreva quando os dois tipos diferirem.
    /// </summary>
    protected virtual TProjection GetProjection(TAggregate entity)
        => entity as TProjection
        ?? throw new InvalidOperationException(
            $"Override {nameof(GetProjection)} when TAggregate ({typeof(TAggregate).Name}) " +
            $"differs from TProjection ({typeof(TProjection).Name}).");

    public async Task Handle(TCommand command, CancellationToken cancellationToken)
    {
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = await ExecuteAsync(command, cancellationToken);

            var hasErrors = await Validate(command, cancellationToken, entity, tx); 
            
            if (hasErrors) return;

            var projection = GetProjection(entity);

            await outboxWriter.WriteAsync(entity, projection, command, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            var payload = JsonSerializer.SerializeToElement(projection, SerializeUtils.EntityJsonOptions);

            await taskCache.SetSuccessAsync(command.TaskId, payload, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);

            await taskCache.SetFailureAsync(command.TaskId, [localizer[MessageKeys.Exception.InternalError].Value], cancellationToken);

            throw;
        }
    }

    private async Task<bool> Validate(TCommand command, CancellationToken cancellationToken, TAggregate? entity,
        IDbTransaction tx)
    {
        if (entity is null)
            AddNotification("",localizer[MessageKeys.Validation.EntityNotFound].Value);

        if (entity is not null && entity.IsFailure)
            AddNotifications(entity);
        
        if (IsValid) return false;
        
        var errors = Notifications.Select(n => $"{n.Key} {n.Message}".Trim()).ToArray();
                
        await taskCache.SetFailureAsync(command.TaskId, errors, cancellationToken);

        await tx.RollbackAsync(cancellationToken);

        return true;

    }
}
