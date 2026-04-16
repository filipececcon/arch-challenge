
namespace ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;

public sealed record TransactionProcessedMessage(string Payload) : MessageBase(EventName)
{
    public new const string EventName = "TransactionProcessed";
}
