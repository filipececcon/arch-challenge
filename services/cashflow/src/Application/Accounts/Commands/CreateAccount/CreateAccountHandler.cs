using ArchChallenge.CashFlow.Application.Common.Audit;
using ArchChallenge.CashFlow.Domain.Shared.Exceptions;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountHandler(
    IReadRepository<Account>   readRepository,
    IWriteRepository<Account>  writeRepository,
    IUnitOfWork                unitOfWork,
    IOutboxRepository          outboxRepository,
    IStringLocalizer<Messages> localizer)
    : IRequestHandler<CreateAccountCommand, CreateAccountResult>
{
    public const string EventName = "AccountCreated";

    public async Task<CreateAccountResult> Handle(CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var existing = await readRepository.FirstOrDefaultAsync(
            new AccountByUserIdSpec(command.UserId),
            cancellationToken);

        if (existing is not null)
            throw new DomainException(localizer[MessageKeys.Validation.AccountAlreadyExists]);

        var account = new Account(command.UserId);

        await writeRepository.AddAsync(account, cancellationToken);

        var auditJson = AuditPayloadBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt);
        await outboxRepository.AddAsync(Outbox.ForAudit(EventName, auditJson), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateAccountResult(
            account.Id,
            account.UserId,
            account.Balance,
            account.CreatedAt);
    }
}
