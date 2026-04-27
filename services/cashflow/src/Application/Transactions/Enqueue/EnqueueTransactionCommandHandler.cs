using ArchChallenge.CashFlow.Application.Abstractions.Messaging;
using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Transactions.Enqueue;

public sealed class EnqueueTransactionCommandHandler(IEventBus eventBus)
    : IRequestHandler<EnqueueTransactionCommand, Result<EnqueueTransactionResult>>
{
    public async Task<Result<EnqueueTransactionResult>> Handle(EnqueueTransactionCommand request, CancellationToken cancellationToken)
    {
        var message = request.BuildMessage();

        await eventBus.PublishAsync(message, cancellationToken);

        return Result<EnqueueTransactionResult>.Ok(new EnqueueTransactionResult(request.TaskId), 202);
    }
}
