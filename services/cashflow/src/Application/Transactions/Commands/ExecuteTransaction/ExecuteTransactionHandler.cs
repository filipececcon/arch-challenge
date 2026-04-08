using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Interfaces;
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
    ITaskCacheService taskCache)
    : IRequestHandler<ExecuteTransactionCommand>
{
    public async Task Handle(ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = new Transaction(command.Type, command.Amount, command.Description);

            if (!entity.IsValid)
            {
                var errors = string.Join("; ", entity.Notifications.Select(n => $"{n.Key}: {n.Message}"));
                await taskCache.SetFailureAsync(command.TaskId, errors, cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            await repository.AddAsync(entity, cancellationToken);

            var payload = JsonSerializer.Serialize(new
            {
                id          = entity.Id.ToString(),
                type        = entity.Type.ToString(),
                amount      = entity.Amount,
                description = entity.Description,
                createdAt   = entity.CreatedAt
            });

            var outboxEvent = new OutboxEvent("TransactionCreated", payload);
            await outboxRepository.AddAsync(outboxEvent, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await taskCache.SetSuccessAsync(command.TaskId, new
            {
                id          = entity.Id,
                type        = entity.Type.ToString(),
                amount      = entity.Amount,
                description = entity.Description,
                createdAt   = entity.CreatedAt
            }, cancellationToken);

            await publisher.Publish(
                new DomainEventNotification<TransactionDoneEvent>(
                    new TransactionDoneEvent(entity)),
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await taskCache.SetFailureAsync(
                command.TaskId,
                "An unexpected error occurred while creating the transaction.",
                cancellationToken);
            throw;
        }
    }
}
