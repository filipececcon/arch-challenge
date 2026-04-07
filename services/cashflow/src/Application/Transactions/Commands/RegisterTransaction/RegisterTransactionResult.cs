using ArchChallenge.CashFlow.Domain.Enums;

namespace ArchChallenge.CashFlow.Application.Transactions.Commands.RegisterTransaction;

public class RegisterTransactionResult(Guid id, TransactionType type, decimal amount, string? description, DateTime createdAt) : Result
{
    public Guid Id = id;
    public TransactionType Type = type;
    public decimal Amount = amount;
    public string? Description = description;
    public DateTime CreatedAt = createdAt;
}