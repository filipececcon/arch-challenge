using ArchChallenge.CashFlow.Application.Common.Interfaces;
using ArchChallenge.CashFlow.Application.Common.Tasks;

namespace ArchChallenge.CashFlow.Application.Common.Enqueue;

/// <summary>
/// Handler genérico e reutilizável para o fluxo EDA de enqueue.
/// Qualquer command que implemente <see cref="IEnqueueCommand{TMessage}"/> é
/// processado aqui sem precisar de um handler próprio:
///   1. Gera o taskId e registra como Pending no cache
///   2. Constrói e publica a mensagem no broker
///   3. Retorna <see cref="EnqueueResult"/> com o taskId para o cliente acompanhar via SSE
/// </summary>
public sealed class EnqueueCommandHandler<TCommand, TMessage>(ITaskCacheService taskCache, IEventBus eventBus)
    : IRequestHandler<TCommand, EnqueueResult>
    where TCommand : class, IEnqueueCommand<TMessage>
    where TMessage : class
{
    public async Task<EnqueueResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var taskId = Guid.NewGuid();

        await taskCache.SetPendingAsync(taskId, cancellationToken);

        var message = request.BuildMessage(taskId);
        
        await eventBus.PublishAsync(message, cancellationToken);

        return new EnqueueResult(taskId);
    }
}
