using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Events;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces;
using ArchChallenge.CashFlow.Domain.Shared.Notifications;

namespace ArchChallenge.CashFlow.Domain.Entities;

public class Transaction : Entity, IAggregateRoot
{
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }

    private readonly List<TransactionRegisteredEvent> _events = [];
    public IReadOnlyList<TransactionRegisteredEvent> Events => _events.AsReadOnly();

    private Transaction() { }

    public static Result<Transaction> Create(TransactionType type, decimal amount, string? description = null)
    {
        var errors = new List<Notification>();

        if (amount <= 0)
            errors.Add(Notification.Create(nameof(Amount), "Transaction amount must be greater than zero."));

        if (description?.Length > 255)
            errors.Add(Notification.Create(nameof(Description), "Description cannot exceed 255 characters."));

        if (errors.Count > 0)
            return Result<Transaction>.Failure(errors);

        var transaction = new Transaction
        {
            Type = type,
            Amount = amount,
            Description = description
        };

        transaction._events.Add(new TransactionRegisteredEvent(
            transaction.Id,
            transaction.Type,
            transaction.Amount,
            transaction.Description));

        return Result<Transaction>.Success(transaction);
    }

    public void ClearEvents() => _events.Clear();
}
