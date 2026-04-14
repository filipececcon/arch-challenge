using System.Linq.Expressions;
using ArchChallenge.CashFlow.Application.Transactions.Queries.GetTransactionById;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Interfaces.Repository;
using ArchChallenge.CashFlow.Domain.Shared.Query;
using ArchChallenge.CashFlow.Domain.Shared.ReadModels;

namespace ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;

/// <summary>
/// Lista transações no Mongo com critérios opcionais montados a partir da query string.
/// </summary>
public sealed class GetAllTransactionsHandler(IDocumentsReadRepository<TransactionDocument> documentsRepository)
    : IRequestHandler<GetAllTransactionsQuery, GetAllTransactionsResult>
{
    public async Task<GetAllTransactionsResult> Handle(GetAllTransactionsQuery request, CancellationToken cancellationToken)
    {
        var criteria = BuildCriteria(request);

        var documents = await documentsRepository.ListAsync(
            predicate: criteria,
            orderBy: d => d.CreatedAt,
            descending: true,
            cancellationToken: cancellationToken);

        var list = documents.Select(GetTransactionByIdFactory.Create).ToList();

        return new GetAllTransactionsResult(list);
    }

    private static Expression<Func<TransactionDocument, bool>>? BuildCriteria(GetAllTransactionsQuery request)
    {
        var q = new QueryCriteriaBuilder<TransactionDocument>();

        if (!string.IsNullOrWhiteSpace(request.Type)
            && Enum.TryParse<TransactionType>(request.Type, ignoreCase: true, out var txType))
        {
            var typeString = txType.ToString();
            q.Where(d => d.Type == typeString);
        }

        q.AndIf(request.Active.HasValue, d => d.Active == request.Active!.Value);
        q.AndIf(request.MinAmount.HasValue, d => d.Amount >= request.MinAmount!.Value);
        q.AndIf(request.MaxAmount.HasValue, d => d.Amount <= request.MaxAmount!.Value);
        q.AndIf(request.CreatedFrom.HasValue, d => d.CreatedAt >= request.CreatedFrom!.Value);
        q.AndIf(request.CreatedTo.HasValue, d => d.CreatedAt <= request.CreatedTo!.Value);

        return q.Build();
    }
}
