using ArchChallenge.CashFlow.Application.Abstractions.Audit;
using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Accounts.Create;

public sealed class CreateAccountCommandHandler(
    IOutboxRepository outboxRepository,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account> readRepository,
    IWriteRepository<Account> writeRepository)
    : IRequestHandler<CreateAccountCommand, Result<CreateAccountResult>>
{
    private const string EventName = "AccountCreated";

    public async Task<Result<CreateAccountResult>> Handle(
        CreateAccountCommand command, CancellationToken cancellationToken)
    {
        var existing = await readRepository.FirstOrDefaultAsync(
            new AccountByUserIdSpec(command.UserId), cancellationToken);

        if (existing is not null)
            return Result<CreateAccountResult>.Fail(409, localizer[MessageKeys.Validation.AccountAlreadyExists].Value);

        var account = new Account(command.UserId);
        await writeRepository.AddAsync(account, cancellationToken);

        var auditJson = AuditPayloadBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt);
        await outboxRepository.AddAsync(Outbox.ForAudit(EventName, auditJson), cancellationToken);

        return Result<CreateAccountResult>.Ok(
            new CreateAccountResult(account.Id, account.UserId, account.Balance, account.CreatedAt),
            201);
    }
}
