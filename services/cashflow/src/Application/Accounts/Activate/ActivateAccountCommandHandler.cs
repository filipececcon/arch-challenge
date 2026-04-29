using System.Text.Json;
using ArchChallenge.CashFlow.Application.Abstractions.Outbox;
using ArchChallenge.CashFlow.Application.Abstractions.Responses;
using ArchChallenge.CashFlow.Application.Abstractions.Results;
using ArchChallenge.CashFlow.Application.Abstractions.Utils;
using ArchChallenge.CashFlow.Application.Accounts.Audit;
using ArchChallenge.Contracts.Events;

namespace ArchChallenge.CashFlow.Application.Accounts.Activate;

public sealed class ActivateAccountCommandHandler(
    IOutboxContext outboxContext,
    IStringLocalizer<Messages> localizer,
    IReadRepository<Account> readRepository,
    IWriteRepository<Account> writeRepository)
    : IRequestHandler<ActivateAccountCommand, Result<NoContentResponse>>
{
    private const string EventName = "AccountActivated";

    public async Task<Result<NoContentResponse>> Handle(ActivateAccountCommand command, CancellationToken cancellationToken)
    {
        var spec = new AccountByUserIdSpec(command.UserId);
        
        var account = await readRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (account is null)
            return Result<NoContentResponse>.NotFound(localizer[MessageKeys.Validation.Account.NotFound].Value);

        if (account.Active)
            return Result<NoContentResponse>.Fail(409, localizer[MessageKeys.Validation.Account.AlreadyExists].Value);

        account.Activate();
        
        await writeRepository.UpdateAsync(account, cancellationToken);

        outboxContext.AddAudit(EventName, AccountAuditBuilder.ForAccount(account, EventName, command.UserId, command.OccurredAt));

        outboxContext.AddEvent(EventName,
            JsonSerializer.Serialize(
                new AccountActivatedIntegrationEvent(account.Id, EventName, command.OccurredAt,
                    new AccountActivatedPayload(account.Id, account.UserId, account.UpdatedAt)),
                SerializeUtils.EntityJsonOptions));

        return Result<NoContentResponse>.Ok(NoContentResponse.Value, 204);
    }
}
