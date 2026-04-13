using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Channels;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Attributes;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Common.Consumer;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Consumers;

[ConsumerChannel<TransactionCreateChannel>]
public sealed class ExecuteTransactionConsumer(ISender sender)
    : CommandConsumerBase<EnqueueTransactionMessage, ExecuteTransactionCommand>(sender)
{
    protected override ExecuteTransactionCommand BuildCommand(EnqueueTransactionMessage msg)
        => new(msg.TaskId, msg.Type, msg.Amount, msg.Description);
}
