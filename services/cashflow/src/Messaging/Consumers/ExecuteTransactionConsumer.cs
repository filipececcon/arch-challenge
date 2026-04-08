using ArchChallenge.CashFlow.Application.Transactions.Commands.CreateTransaction;
using ArchChallenge.CashFlow.Application.Transactions.Commands.ExecuteTransaction;

namespace ArchChallenge.CashFlow.Infrastructure.CrossCutting.Messaging.Consumers;

public sealed class ExecuteTransactionConsumer(ISender sender) : IConsumer<CreateTransactionMessage>
{
    public Task Consume(ConsumeContext<CreateTransactionMessage> context)
    {
        var msg = context.Message;

        var cmd = new ExecuteTransactionCommand(msg.TaskId, msg.Type, msg.Amount, msg.Description);
        
        return sender.Send(cmd, context.CancellationToken);
    }
}
