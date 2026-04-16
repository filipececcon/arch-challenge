namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public sealed class ExecuteTransactionHandler(
    IWriteRepository<Transaction> repository,
    IOutboxRepository             outboxRepository,
    IUnitOfWork                   unitOfWork,
    IAuditContext                 auditContext,
    ITaskCacheService             taskCache,
    IEventBus                     eventBus,
    IStringLocalizer<Messages>    localizer)
    : CommandHandlerBase<ExecuteTransaction, Transaction, TransactionProcessedMessage>(
        unitOfWork, outboxRepository, auditContext, taskCache, eventBus, localizer)
{
    
    protected override async Task<Transaction> ExecuteAsync(
        ExecuteTransaction command, CancellationToken cancellationToken)
    {
        var entity = new Transaction(command.Type, command.Amount, command.Description);
       
        await repository.AddAsync(entity, cancellationToken);

        return entity;
    }
}
