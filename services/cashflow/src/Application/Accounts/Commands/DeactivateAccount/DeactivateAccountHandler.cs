using ArchChallenge.CashFlow.Application.Common.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Exceptions;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.DeactivateAccount;

public sealed class DeactivateAccountHandler(
    IReadRepository<Account>   readRepository,
    IWriteRepository<Account>  writeRepository,
    IUnitOfWork                unitOfWork,
    IOutboxRepository          outboxRepository,
    IStringLocalizer<Messages> localizer)
    : IRequestHandler<DeactivateAccountCommand, bool>
{
    private const string EventName = "AccountDeactivated";

    public async Task<bool> Handle(DeactivateAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await readRepository.FirstOrDefaultAsync(
            new AccountByUserIdSpec(command.UserId),
            cancellationToken);

        if (account is null)
            return false;

        if (!account.Active)
            throw new DomainException(localizer[MessageKeys.Validation.AccountNotFound]);

        account.Deactivate();

        await writeRepository.UpdateAsync(account, cancellationToken);

        var auditJson = AuditPayloadBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt);
        await outboxRepository.AddAsync(Outbox.ForAudit(EventName, auditJson), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
