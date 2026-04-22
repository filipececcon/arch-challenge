namespace ArchChallenge.CashFlow.Application.Common.Enqueue;

/// <summary>
/// Handler genérico e reutilizável para o fluxo EDA de enqueue.
/// Qualquer command que implemente <see cref="IEnqueueCommand{TMessage}"/> é
/// processado aqui sem precisar de um handler próprio:
///   1. Verifica idempotência (retorna taskId existente se a chave já foi usada).
///   2. Gera o taskId e registra como Pending no cache.
///   3. Constrói e publica a mensagem no broker.
///   4. Persiste a associação idempotencyKey → taskId no cache.
///   5. Retorna <see cref="EnqueueResult"/> com o taskId para o cliente acompanhar via SSE.
/// </summary>
public sealed class EnqueueCommandHandler<TCommand, TMessage>(ITaskCacheService taskCache, IEventBus eventBus)
    : IRequestHandler<TCommand, EnqueueResult>
    where TCommand : class, IEnqueueCommand<TMessage>
    where TMessage : class
{
    public async Task<EnqueueResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        // Idempotência: se a chave já existir no cache, devolve o taskId original.
        if (request.IdempotencyKey is { } key)
        {
            var existingTaskId = await taskCache.GetIdempotencyAsync(key, cancellationToken);
            if (existingTaskId is not null)
                return new EnqueueResult(existingTaskId.Value);
        }

        var taskId = Guid.NewGuid();

        await taskCache.SetPendingAsync(taskId, cancellationToken);

        var message = request.BuildMessage(taskId);

        await eventBus.PublishAsync(message, cancellationToken);

        if (request.IdempotencyKey is { } newKey)
            await taskCache.SetIdempotencyAsync(newKey, taskId, cancellationToken);

        return new EnqueueResult(taskId);
    }
}
