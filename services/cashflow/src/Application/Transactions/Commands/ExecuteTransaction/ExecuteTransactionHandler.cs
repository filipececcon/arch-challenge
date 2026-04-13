using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Notifications;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Domain.Events;
using ArchChallenge.CashFlow.Domain.Shared.Events;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public class ExecuteTransactionHandler(
    IWriteRepository<Transaction> repository,
    IOutboxRepository outboxRepository,
    IPublisher publisher,
    IUnitOfWork unitOfWork,
    ITaskCacheService taskCache,
    IStringLocalizer<Messages> localizer)
    : IRequestHandler<ExecuteTransactionCommand>
{
    public async Task Handle(ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        await using var dbTransaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = new Transaction(command.Type, command.Amount, command.Description);

            if (!entity.IsValid)
            {
                await taskCache.SetFailureAsync(command.TaskId, localizer[MessageKeys.Exception.DomainError], cancellationToken);
                
                await dbTransaction.RollbackAsync(cancellationToken);
                
                return;
            }

            await repository.AddAsync(entity, cancellationToken);

            var payload = JsonSerializer.Serialize(entity);

            var @event = new TransactionProcessedEvent(payload);

            var outboxEvent = new OutboxEvent(@event.EventName, payload);
            
            await outboxRepository.AddAsync(outboxEvent, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            await dbTransaction.CommitAsync(cancellationToken);

            await taskCache.SetSuccessAsync(command.TaskId, JsonDocument.Parse(payload).RootElement, cancellationToken);

            await publisher.Publish(new DomainEventNotification<TransactionProcessedEvent>(@event), cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);

            await taskCache.SetFailureAsync(command.TaskId, localizer[MessageKeys.Exception.InternalError], cancellationToken);
            
            throw;
        }
    }
}
