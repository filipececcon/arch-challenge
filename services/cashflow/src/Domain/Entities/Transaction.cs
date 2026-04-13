using ArchChallenge.CashFlow.Domain.Contracts;

namespace ArchChallenge.CashFlow.Domain.Entities;

public class Transaction : Entity, IAggregateRoot
{
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }

    private Transaction() { }

    public Transaction(TransactionType type, decimal amount, string? description = null)
    {
        Type = type;
        Amount = amount;
        Description = description;

        AddNotifications(new EnqueueTransactionContract(this));
    }
}
