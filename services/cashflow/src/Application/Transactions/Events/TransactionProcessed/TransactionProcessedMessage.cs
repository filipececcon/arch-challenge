namespace ArchChallenge.CashFlow.Application.Transactions.Events.TransactionProcessed;

public sealed record TransactionProcessedMessage(string Payload)
{
    public static string EventName = "TransactionProcessed";
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string Payload { get; } = Payload;
}
