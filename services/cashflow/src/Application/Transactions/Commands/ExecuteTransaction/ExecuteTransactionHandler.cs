namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

/// <summary>
/// Handler para execução de transação.
/// <list type="bullet">
///   <item><c>TAggregate  = Account</c>     — raiz de agregação; valida e persiste o movimento.</item>
///   <item><c>TProjection = Transaction</c> — último filho do agregado; projetado no Mongo e no task-cache.</item>
/// </list>
/// O <see cref="Transaction"/> é adicionado via <c>Account.AddTransaction</c> e persistido pelo EF Core
/// como filho do agregado (cascade save via navigation property — sem IWriteRepository&lt;Transaction&gt;).
/// </summary>
public sealed class ExecuteTransactionHandler(
    IUnitOfWork                unitOfWork,
    IOutboxRepository          outboxRepository,
    ITaskCacheService          taskCache,
    IStringLocalizer<Messages> localizer,
    IWriteRepository<Account>  accountRepository,
    IOutboxMapper<ExecuteTransactionCommand, Account, Transaction> outboxMapper)
    : CommandHandlerBase<ExecuteTransactionCommand, Account, Transaction>(
        unitOfWork, outboxRepository, taskCache, localizer, outboxMapper)
{
    protected override async Task<Account> ExecuteAsync(
        ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        var spec = new AccountByUserIdSpec(command.UserId);

        var account = await accountRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is null)
        {
            account = new Account(command.UserId);

            await accountRepository.AddAsync(account, cancellationToken);
        }

        var transaction = new Transaction(command.Type, command.Amount, command.Description);

        account.AddTransaction(transaction);

        return account;
    }

    protected override Transaction GetProjection(Account entity) => entity.Transactions[^1];
}
