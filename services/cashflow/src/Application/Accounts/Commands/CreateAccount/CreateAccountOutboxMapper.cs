using ArchChallenge.CashFlow.Application.Common.Audit;
using ArchChallenge.CashFlow.Application.Common.Outbox;

namespace ArchChallenge.CashFlow.Application.Accounts.Commands.CreateAccount;

public sealed class CreateAccountOutboxMapper : OutboxMapperBase<CreateAccountCommand, Account, Account>
{
    public override string EventName => "AccountCreated";

    public override string? ToAudit(Account entity, CreateAccountCommand command) =>
        AuditPayloadBuilder.ForAccount(
            entity,
            EventName,
            command.UserId,
            command.OccurredAt);

    public override string? ToEvents(Account projection, CreateAccountCommand command) => null;
}
