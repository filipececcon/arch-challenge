using System.Text.Json;
using ArchChallenge.CashFlow.Application.Common.Audit;
using ArchChallenge.CashFlow.Application.Common.Outbox;
using ArchChallenge.CashFlow.Application.Utils;
using ArchChallenge.Contracts.Events;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

public sealed class ExecuteTransactionOutboxMapper
    : OutboxMapperBase<ExecuteTransactionCommand, Account, Transaction>
{
    /// <summary>Nome canônico do evento; use para lookups estáticos (outbox, type map).</summary>
    public const string EventNameValue = "TransactionExecuted";

    public override string EventName => EventNameValue;
    
    public override string? ToAudit(Account entity, ExecuteTransactionCommand command)
        => AuditPayloadBuilder.ForAccount(
            entity,
            EventName,
            command.UserId,
            command.OccurredAt,
            relatedTransactionId: entity.Transactions[^1].Id);

    public override string? ToEvents(Transaction projection, ExecuteTransactionCommand command)
    {
        var integrationEvent = new TransactionRegisteredIntegrationEvent(
            projection.Id,
            EventName,
            command.OccurredAt,
            new TransactionRegisteredPayload(
                projection.Type.ToString().ToUpperInvariant(),
                projection.Amount,
                projection.AccountId,
                projection.BalanceAfter,
                projection.Description,
                command.UserId));

        return JsonSerializer.Serialize(integrationEvent, SerializeUtils.EntityJsonOptions);
    }
}
