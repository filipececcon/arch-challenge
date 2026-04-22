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

    public async Task<DailyBalanceDto?> GetByDateAsync(DateOnly date, string userId, CancellationToken cancellationToken)
    {
        var dayId = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var filter = Builders<DailyConsolidationDocument>.Filter.And(
            Builders<DailyConsolidationDocument>.Filter.Eq(d => d.UserId, userId),
            Builders<DailyConsolidationDocument>.Filter.Eq(d => d.Day, dayId));

        var row = await _daily.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<IReadOnlyList<DailyBalanceDto>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        string? userId,
        CancellationToken cancellationToken)
    {
        var fromId = from?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toId   = to?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var q = new QueryCriteriaBuilder<DailyConsolidationDocument>();
        if (!string.IsNullOrEmpty(userId))
            q.Where(d => d.UserId == userId);

        var filter = q
            .AndIf(fromId is not null, d => string.CompareOrdinal(d.Day, fromId) >= 0)
            .AndIf(toId   is not null, d => string.CompareOrdinal(d.Day, toId)   <= 0)
            .Build();

        var rows = await _daily
            .Find(filter ?? (_ => true))
            .SortBy(d => d.Day)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    private static DailyBalanceDto ToDto(DailyConsolidationDocument row)
    {
        var date = DateOnly.ParseExact(row.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new DailyBalanceDto(date, row.TotalCredits, row.TotalDebits, row.TotalCredits - row.TotalDebits);
    }
}
