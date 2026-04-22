using ArchChallenge.CashFlow.Application.Common.Outbox;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

/// <summary>
/// Manipula a execução de uma transação (débito ou crédito) na conta do usuário, garantindo que a operação seja atômica e consistente.
/// </summary>
/// <param name="unitOfWork"></param>
/// <param name="outboxRepository"></param>
/// <param name="taskCache"></param>
/// <param name="localizer"></param>
/// <param name="readRepository"></param>
/// <param name="outboxMapper"></param>
public sealed class ExecuteTransactionHandler(
    IUnitOfWork                unitOfWork,
    IOutboxRepository          outboxRepository,
    ITaskCacheService          taskCache,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account>   readRepository,
    IOutboxMapper<ExecuteTransactionCommand, Account, Transaction> outboxMapper)
    : AsyncCommandHandlerBase<ExecuteTransactionCommand, Account, Transaction>(
        unitOfWork, outboxRepository, taskCache, localizer, outboxMapper,
        new OutboxWriter<ExecuteTransactionCommand, Account, Transaction>(outboxRepository, outboxMapper))
{
    protected override async Task<Account?> ExecuteAsync(
        ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        var spec = new AccountByUserIdSpec(command.UserId);

        var account = await readRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is not null)
        {
            var transaction = new Transaction(command.Type, command.Amount, command.Description);

            account.AddTransaction(transaction);
        }

        return account;
    }

    protected override Transaction GetProjection(Account entity) => entity.Transactions[^1];
}
