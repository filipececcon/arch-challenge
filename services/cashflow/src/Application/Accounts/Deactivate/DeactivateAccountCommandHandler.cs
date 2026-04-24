using ArchChallenge.CashFlow.Application.Abstractions.Audit;
using ArchChallenge.CashFlow.Application.Abstractions.Responses;
using ArchChallenge.CashFlow.Application.Abstractions.Results;

namespace ArchChallenge.CashFlow.Application.Accounts.Deactivate;

public sealed class DeactivateAccountCommandHandler(
    IOutboxRepository outboxRepository,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account> readRepository,
    IWriteRepository<Account> writeRepository)
    : IRequestHandler<DeactivateAccountCommand, Result<NoContentResponse>>
{
    private const string EventName = "AccountDeactivated";

    public async Task<Result<NoContentResponse>> Handle(
        DeactivateAccountCommand command, CancellationToken cancellationToken)
    {
        var account = await readRepository.FirstOrDefaultAsync(
            new AccountByUserIdSpec(command.UserId), cancellationToken);

        if (account is null)
            return Result<NoContentResponse>.NotFound(localizer[MessageKeys.Validation.EntityNotFound].Value);

        if (!account.Active)
            return Result<NoContentResponse>.Fail(409, localizer[MessageKeys.Validation.AccountAlreadyExists].Value);

        account.Deactivate();
        await writeRepository.UpdateAsync(account, cancellationToken);

        var auditJson = AuditPayloadBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt);
        await outboxRepository.AddAsync(Outbox.ForAudit(EventName, auditJson), cancellationToken);

        return Result<NoContentResponse>.Ok(NoContentResponse.Value, 204);
    }
}
