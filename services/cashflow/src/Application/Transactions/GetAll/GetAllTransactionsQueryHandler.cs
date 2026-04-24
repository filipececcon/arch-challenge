using System.Linq.Expressions;
using ArchChallenge.CashFlow.Application.Transactions.GetById;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Criteria;
using ArchChallenge.CashFlow.Infrastructure.Data.Documents.Models;

namespace ArchChallenge.CashFlow.Application.Transactions.GetAll;

public sealed class GetAllTransactionsQueryHandler(
    IDocumentsReadRepository<TransactionDocument> documentsRepository,
    IReadRepository<Account>                        accountRepository)
    : IRequestHandler<GetAllTransactionsQuery, GetAllTransactionsResult>
{
    public async Task<GetAllTransactionsResult> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.FirstOrDefaultAsync(
            new AccountByUserIdSpec(request.UserId),
            cancellationToken);

        if (account is null)
            return new GetAllTransactionsResult([]);

        var criteria = BuildCriteria(request, account.Id);

        var documents = await documentsRepository.ListAsync(
            predicate: criteria,
            orderBy: d => d.CreatedAt,
            descending: true,
            cancellationToken: cancellationToken);

        var transactions = documents.Select(GetTransactionByIdFactory.Create).ToList();

        return new GetAllTransactionsResult(transactions);
    }

    private static Expression<Func<TransactionDocument, bool>>? BuildCriteria(
        GetAllTransactionsQuery request,
        Guid accountId)
    {
        var q = new QueryCriteriaBuilder<TransactionDocument>();

        q.Where(d => d.AccountId == accountId);

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var txType = Enum.Parse<TransactionType>(request.Type, ignoreCase: true);
            q.Where(d => d.Type == txType.ToString());
        }

        q.AndIf(request.Active.HasValue, d => d.Active == request.Active!.Value);
        q.AndIf(request.MinAmount.HasValue, d => d.Amount >= request.MinAmount!.Value);
        q.AndIf(request.MaxAmount.HasValue, d => d.Amount <= request.MaxAmount!.Value);
        q.AndIf(request.CreatedFrom.HasValue, d => d.CreatedAt >= request.CreatedFrom!.Value);
        q.AndIf(request.CreatedTo.HasValue, d => d.CreatedAt <= request.CreatedTo!.Value);

        return q.Build();
    }
}
