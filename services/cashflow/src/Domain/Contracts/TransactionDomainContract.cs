using Flunt.Validations;

namespace ArchChallenge.CashFlow.Domain.Contracts;

public class TransactionDomainContract : Contract<Transaction>
{
    public TransactionDomainContract(Transaction transaction)
    {
        Requires()
            .IsGreaterThan(transaction.Amount, 0, nameof(transaction.Amount), 
                "Transaction amount must be greater than zero.");

        if (transaction.Description != null)
            IsLowerOrEqualsThan(transaction.Description.Length, 255, nameof(transaction.Description),
                "Description cannot exceed 255 characters.");
    }
}