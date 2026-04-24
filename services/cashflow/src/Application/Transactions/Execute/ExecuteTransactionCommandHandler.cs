using System.Text.Json;
using ArchChallenge.CashFlow.Application.Abstractions.Audit;
using ArchChallenge.CashFlow.Application.Abstractions.Tasks;
using ArchChallenge.CashFlow.Application.Abstractions.Utils;
using ArchChallenge.Contracts.Events;

namespace ArchChallenge.CashFlow.Application.Transactions.Execute;

public sealed class ExecuteTransactionCommandHandler(
    IUnitOfWork unitOfWork,
    IOutboxRepository outboxRepository,
    ITaskCacheService taskCache,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account> readRepository)
    : IRequestHandler<ExecuteTransactionCommand>
{
    public const string EventName = "TransactionExecuted";

    public async Task Handle(ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        await using var tx = await unitOfWork.BeginTransactionAsync(cancellationToken);

        Transaction? transaction = null;

        try
        {
            var account = await readRepository.FirstOrDefaultAsync(
                new AccountByUserIdSpec(command.UserId), cancellationToken);

            if (account is null)
            {
                await tx.RollbackAsync(cancellationToken);
                
                await taskCache.SetFailureAsync(command.TaskId,
                    [localizer[MessageKeys.Validation.EntityNotFound].Value], cancellationToken);
                
                return;
            }

            transaction = new Transaction(command.Type, command.Amount, command.Description);
            
            account.AddTransaction(transaction);

            if (account.IsFailure)
            {
                var errors = account.Notifications.Select(n => $"{n.Key} {n.Message}".Trim()).ToArray();
                await tx.RollbackAsync(cancellationToken);
                await taskCache.SetFailureAsync(command.TaskId, errors, cancellationToken);
                return;
            }

            var mongoJson = JsonSerializer.Serialize(transaction, transaction.GetType(), SerializeUtils.EntityJsonOptions);
            await outboxRepository.AddAsync(Outbox.ForMongo(EventName, mongoJson), cancellationToken);

            var auditJson = AuditPayloadBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt, relatedTransactionId: transaction.Id);
            await outboxRepository.AddAsync(Outbox.ForAudit(EventName, auditJson), cancellationToken);

            var integrationEvent = new TransactionRegisteredIntegrationEvent(
                transaction.Id, EventName, command.OccurredAt,
                new TransactionRegisteredPayload(
                    transaction.Type.ToString().ToUpperInvariant(),
                    transaction.Amount,
                    transaction.AccountId,
                    transaction.BalanceAfter,
                    transaction.Description,
                    command.UserId));
            
            var eventsJson = JsonSerializer.Serialize(integrationEvent, SerializeUtils.EntityJsonOptions);
            await outboxRepository.AddAsync(Outbox.ForEvents(EventName, eventsJson), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var payload = JsonSerializer.SerializeToElement(transaction, SerializeUtils.EntityJsonOptions);
            await taskCache.SetSuccessAsync(command.TaskId, payload, cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            await taskCache.SetFailureAsync(command.TaskId,
                [localizer[MessageKeys.Exception.InternalError].Value], cancellationToken);
            throw;
        }
    }
}
