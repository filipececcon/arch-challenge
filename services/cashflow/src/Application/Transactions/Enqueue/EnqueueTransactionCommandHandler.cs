using ArchChallenge.CashFlow.Application.Abstractions.Messaging;
using ArchChallenge.CashFlow.Application.Abstractions.Tasks;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public sealed class EnqueueTransactionCommandHandler(ITaskCacheService taskCache, IEventBus eventBus)
    : IRequestHandler<EnqueueTransactionCommand, EnqueueTransactionResult>
{
    public async Task<EnqueueTransactionResult> Handle(EnqueueTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey is { } key)
        {
            var existingTaskId = await taskCache.GetIdempotencyAsync(key, cancellationToken);
            
            if (existingTaskId is not null) return new EnqueueTransactionResult(existingTaskId.Value);
        }

        var taskId = Guid.NewGuid();

        await taskCache.SetPendingAsync(taskId, cancellationToken);

        var message = request.BuildMessage(taskId);

        await eventBus.PublishAsync(message, cancellationToken);

        if (request.IdempotencyKey is { } newKey)
            await taskCache.SetIdempotencyAsync(newKey, taskId, cancellationToken);

        return new EnqueueTransactionResult(taskId);
    }
}
