using System.Text.Json;
using System.Text.Json.Serialization;
using ArchChallenge.CashFlow.Application.Common.Tasks;
using ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;
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
    private static readonly JsonSerializerOptions EntityJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

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
            
            var json = JsonSerializer.Serialize(entity, EntityJsonOptions);

            var message = new TransactionProcessedMessage(json);
            
            var @event = new TransactionProcessedEvent(message);

            var outboxEvent = new OutboxEvent(TransactionProcessedMessage.EventName, json);
            
            await outboxRepository.AddAsync(outboxEvent, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            
            await dbTransaction.CommitAsync(cancellationToken);

            var payloadElement = JsonSerializer.Deserialize<JsonElement>(json);
            await taskCache.SetSuccessAsync(command.TaskId, payloadElement, cancellationToken);

            await publisher.Publish(@event, cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);

            await taskCache.SetFailureAsync(command.TaskId, localizer[MessageKeys.Exception.InternalError], cancellationToken);
            
            throw;
        }
    }
}
