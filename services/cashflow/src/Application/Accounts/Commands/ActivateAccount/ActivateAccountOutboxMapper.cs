using ArchChallenge.CashFlow.Application.Common.Audit;
using ArchChallenge.CashFlow.Application.Common.Outbox;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.ActivateAccount;

public sealed class ActivateAccountOutboxMapper
    : OutboxMapperBase<ActivateAccountCommand, Account, Account>
{
    public override string EventName => "AccountActivated";

    public override string? ToAudit(Account entity, ActivateAccountCommand command)
        => AuditPayloadBuilder.ForAccount(
            entity,
            EventName,
            command.UserId,
            command.OccurredAt);

    public override string? ToEvents(Account projection, ActivateAccountCommand command) => null;
}
