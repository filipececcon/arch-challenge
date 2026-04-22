using ArchChallenge.CashFlow.Application.Common.Audit;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.ActivateAccount;

public sealed class ActivateAccountHandler(
    IReadRepository<Account>   readRepository,
    IWriteRepository<Account>  writeRepository,
    IUnitOfWork                unitOfWork,
    IOutboxRepository          outboxRepository,
    IStringLocalizer<Messages> localizer)
    : IRequestHandler<ActivateAccountCommand, bool>
{
    private const string EventName = "AccountActivated";

    public async Task<bool> Handle(ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        var spec    = new AccountByUserIdSpec(command.UserId);
        
        var account = await readRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is null) return false;

        if (account.Active)
        {
            account.AddNotification("", localizer[MessageKeys.Validation.AccountAlreadyExists]);
        }

        if (account.IsFailure) return false;

        account.Activate();

        await writeRepository.UpdateAsync(account, cancellationToken);

        var auditJson = AuditPayloadBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt);

        await outboxRepository.AddAsync(Outbox.ForAudit(EventName, auditJson), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
