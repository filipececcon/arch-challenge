using System.Globalization;
using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Application.DailyBalances;
using ArchChallenge.Dashboard.Domain.Shared.Criteria;
using ArchChallenge.Dashboard.Infrastructure.Data.Documents;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Infrastructure.Data.Services;

public class DailyBalanceReadStore(IMongoDatabase database) : IDailyBalanceReadStore
{
    private readonly IMongoCollection<DailyConsolidationDocument> _daily =
        database.GetCollection<DailyConsolidationDocument>(MongoDashboardCollections.DailyConsolidations);

    public async Task<DailyBalanceDto?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var id  = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var row = await _daily.Find(d => d.Id == id).FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<DailyBalanceDto>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var fromId = from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toId   = to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var filter = new QueryCriteriaBuilder<DailyConsolidationDocument>()
            .AndIf(fromId is not null, d => d.Id.CompareTo(fromId) >= 0)
            .AndIf(toId   is not null, d => d.Id.CompareTo(toId)   <= 0)
            .Build();

        var rows = await _daily
            .Find(filter ?? (_ => true))
            .SortBy(d => d.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    private static DailyBalanceDto ToDto(DailyConsolidationDocument row)
    {
        var date = DateOnly.ParseExact(row.Id, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new DailyBalanceDto(date, row.TotalCredits, row.TotalDebits, row.TotalCredits - row.TotalDebits);
    }
}
