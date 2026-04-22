namespace ArchChallenge.CashFlow.Domain.Entities;

/// <summary>
/// Conta corrente do usuário (MVP: uma conta implícita por <c>UserId</c>).
/// Raiz de agregação: <see cref="Transaction"/> é filho deste agregado e
/// só pode ser criado via <see cref="AddTransaction"/>.
/// </summary>
public sealed class Account : Entity, IAggregateRoot
{
    public string UserId { get; private set; } = null!;

    /// <summary>Saldo atual após o último movimento persistido.</summary>
    public decimal Balance { get; private set; }

    private readonly List<Transaction> _transactions = new();

    /// <summary>Lançamentos deste agregado (novos e/ou carregados pelo EF Core).</summary>
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();

    private Account() { }

    public Account(string userId)
    {
        UserId  = userId;
        Balance = 0m;
    }

    /// <summary>
    /// Valida as regras de negócio da conta e registra o movimento.
    /// O <see cref="Transaction"/> é criado externamente e passado aqui — a conta aplica
    /// as invariantes (saldo, tipo), define o <c>BalanceAfter</c> e adiciona à coleção.
    /// Será persistido automaticamente pelo EF Core (cascade via navigation property).
    /// </summary>
    /// <returns>O próprio <paramref name="transaction"/> em caso de sucesso; <c>null</c> se rejeitado.</returns>
    public Transaction? AddTransaction(Transaction transaction)
    {
        if (!transaction.IsValid)
        {
            AddNotifications(transaction.Notifications);

            return null;
        }

        if (transaction.Type == TransactionType.Debit && Balance < transaction.Amount)
        {
            AddNotification(nameof(Balance), "Insufficient balance for debit.");

            return null;
        }

        Balance = transaction.Type == TransactionType.Credit
            ? Balance + transaction.Amount
            : Balance - transaction.Amount;

        transaction.SetBalanceAfter(Balance);
        transaction.SetAccountId(Id);

        _transactions.Add(transaction);

        return transaction;
    }
}
