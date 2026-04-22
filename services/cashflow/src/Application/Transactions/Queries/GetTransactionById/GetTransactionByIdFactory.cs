using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;

public static class GetTransactionByIdFactory
{
    public static GetTransactionByIdResult Create(TransactionDocument document) =>
        new(
            document.Id,
            document.AccountId,
            document.Type,
            document.Amount,
            document.BalanceAfter,
            document.Description,
            document.CreatedAt,
            document.Active);
    
    public static GetTransactionByIdResult Create(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AccountId,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.BalanceAfter,
            transaction.Description,
            transaction.CreatedAt,
            transaction.Active);
}