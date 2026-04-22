using ArchChallenge.CashFlow.Domain.Contracts;

namespace ArchChallenge.CashFlow.Domain.Entities;

public class Transaction : Entity
{
    public Guid AccountId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Saldo da conta após este lançamento (denormalizado para evento/projeção).</summary>
    public decimal BalanceAfter { get; private set; }

    private Transaction() { }

    public Transaction(TransactionType type, decimal amount, string? description = null)
    {
        Type        = type;
        Amount      = amount;
        Description = description;

        AddNotifications(new TransactionDomainContract(this));
    }

    internal void SetAccountId(Guid accountId) => AccountId = accountId;
    
    internal void SetBalanceAfter(decimal balanceAfter) => BalanceAfter = balanceAfter;
}
