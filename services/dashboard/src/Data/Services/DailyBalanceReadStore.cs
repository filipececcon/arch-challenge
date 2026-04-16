using System.Globalization;
using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Application.DailyBalances;
using ArchChallenge.Dashboard.Data.Documents;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Data.Services;

public class DailyBalanceReadStore : IDailyBalanceReadStore
{
    private readonly IMongoCollection<DailyConsolidationDocument> _daily;

    public DailyBalanceReadStore(IMongoDatabase database)
    {
        _daily = database.GetCollection<DailyConsolidationDocument>(MongoDashboardCollections.DailyConsolidations);
    }

    public async Task<DailyBalanceDto?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var id = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var row = await _daily.Find(d => d.Id == id).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<DailyBalanceDto>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var filter = Builders<DailyConsolidationDocument>.Filter.Empty;

        if (from is not null)
        {
            var fromId = from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            filter &= Builders<DailyConsolidationDocument>.Filter.Gte(d => d.Id, fromId);
        }

        if (to is not null)
        {
            var toId = to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            filter &= Builders<DailyConsolidationDocument>.Filter.Lte(d => d.Id, toId);
        }

        var rows = await _daily.Find(filter).SortBy(d => d.Id).ToListAsync(cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    private static DailyBalanceDto ToDto(DailyConsolidationDocument row)
    {
        var date = DateOnly.ParseExact(row.Id, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new DailyBalanceDto(date, row.TotalCredits, row.TotalDebits, row.TotalCredits - row.TotalDebits);
    }
}
