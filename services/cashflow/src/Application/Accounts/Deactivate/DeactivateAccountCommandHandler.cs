using ArchChallenge.CashFlow.Application.Abstractions.Outbox;
using ArchChallenge.CashFlow.Application.Abstractions.Responses;
using ArchChallenge.CashFlow.Application.Abstractions.Results;
using ArchChallenge.CashFlow.Application.Accounts.Audit;

namespace ArchChallenge.CashFlow.Application.Accounts.Deactivate;

public sealed class DeactivateAccountCommandHandler(
    IOutboxContext outboxContext,
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
            return Result<NoContentResponse>.NotFound(localizer[MessageKeys.Validation.Account.NotFound].Value);

        if (!account.Active)
            return Result<NoContentResponse>.Fail(409, localizer[MessageKeys.Validation.Account.AlreadyExists].Value);

        account.Deactivate();
        
        await writeRepository.UpdateAsync(account, cancellationToken);

        outboxContext.AddAudit(EventName, AccountAuditBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt));

        return Result<NoContentResponse>.Ok(NoContentResponse.Value, 204);
    }
}
