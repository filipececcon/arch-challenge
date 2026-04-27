using System.Text.Json;
using ArchChallenge.CashFlow.Application.Abstractions.Outbox;
using ArchChallenge.CashFlow.Application.Abstractions.Results;
using ArchChallenge.CashFlow.Application.Abstractions.Utils;
using ArchChallenge.CashFlow.Application.Accounts.Audit;
using ArchChallenge.Contracts.Events;

namespace ArchChallenge.CashFlow.Application.Transactions.Execute;

public sealed class ExecuteTransactionCommandHandler(
    IOutboxContext outboxContext,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account> readRepository)
    : IRequestHandler<ExecuteTransactionCommand, Result<ExecuteTransactionResult>>
{
    public const string EventName = "TransactionExecuted";

    public async Task<Result<ExecuteTransactionResult>> Handle(
        ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        var (account, result) = await ValidateBusiness(command, cancellationToken);
        
        if (account is null || result.IsFailure) return result;

        var transaction = new Transaction(command.Type, command.Amount, command.Description);
        
        account.AddTransaction(transaction);

        if (account.IsFailure)
        {
            var errors = account.Notifications.Select(n => $"{n.Key} {n.Message}".Trim()).ToArray();
            
            return Result<ExecuteTransactionResult>.Fail(400, errors);
        }

        MakeOutbox(command, transaction, account);

        var data = new ExecuteTransactionResult(
            transaction.Id, transaction.AccountId,
            transaction.Type.ToString(), transaction.Amount,
            transaction.BalanceAfter, transaction.Description,
            transaction.CreatedAt); 
        
        return Result<ExecuteTransactionResult>.Ok(data);
    }

    private void MakeOutbox(ExecuteTransactionCommand command, Transaction transaction, Account account)
    {
        outboxContext.AddMongo(EventName,
            JsonSerializer.Serialize(transaction, transaction.GetType(), SerializeUtils.EntityJsonOptions));

        outboxContext.AddAudit(EventName,
            AccountAuditBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt,
                relatedTransactionId: transaction.Id));

        outboxContext.AddEvent(EventName,
            JsonSerializer.Serialize(
                new TransactionRegisteredIntegrationEvent(
                    transaction.Id, EventName, command.OccurredAt,
                    new TransactionRegisteredPayload(
                        transaction.Type.ToString().ToUpperInvariant(),
                        transaction.Amount,
                        transaction.AccountId,
                        transaction.BalanceAfter,
                        transaction.Description,
                        command.UserId)),
                SerializeUtils.EntityJsonOptions));
    }

    private async Task<(Account? account, Result<ExecuteTransactionResult> result)> ValidateBusiness(ExecuteTransactionCommand command, CancellationToken cancellationToken)
    {
        var spec = new AccountByUserIdSpec(command.UserId); 
        
        var account = await readRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is null)
            return (account, Result<ExecuteTransactionResult>.NotFound(localizer[MessageKeys.Validation.Account.NotFound].Value));
        
        if(!account.Active)
            return (account, Result<ExecuteTransactionResult>.NotFound(localizer[MessageKeys.Validation.Transaction.AccountDeactivated].Value));

        Result<ExecuteTransactionResult>? result = null;
        
        return (account, result)!;
    }
}

