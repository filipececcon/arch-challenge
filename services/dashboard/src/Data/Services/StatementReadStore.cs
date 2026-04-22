using System.Globalization;
using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Application.Statement;
using ArchChallenge.Dashboard.Domain.Shared.Criteria;
using ArchChallenge.Dashboard.Infrastructure.Data.Documents;
using MongoDB.Driver;

namespace ArchChallenge.Dashboard.Infrastructure.Data.Services;

public class StatementReadStore(IMongoDatabase database) : IStatementReadStore
{
    private readonly IMongoCollection<StatementLineDocument> _lines =
        database.GetCollection<StatementLineDocument>(MongoDashboardCollections.StatementLines);

    public async Task<StatementPageDto> ListAsync(
        string userId,
        DateOnly? from,
        DateOnly? to,
        string? type,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var fromUtc  = from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc    = to?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var typeNorm = type?.ToUpperInvariant();

        var filter = new QueryCriteriaBuilder<StatementLineDocument>()
            .Where(l => l.UserId == userId)
            .AndIf(fromUtc   is not null, l => l.OccurredAt >= fromUtc!.Value)
            .AndIf(toUtc     is not null, l => l.OccurredAt <= toUtc!.Value)
            .AndIf(typeNorm  is not null, l => l.Type       == typeNorm!)
            .Build();

        var findFilter = filter ?? (_ => true);

        var totalCount = (int)await _lines.CountDocumentsAsync(findFilter, cancellationToken: cancellationToken);

        var items = await _lines
            .Find(findFilter)
            .SortByDescending(l => l.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new StatementPageDto(items.Select(ToDto).ToList(), totalCount, page, pageSize);
    }

    private static StatementLineDto ToDto(StatementLineDocument doc)
    {
        var date = DateOnly.ParseExact(doc.Day, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new StatementLineDto(doc.Id, doc.AccountId, date, doc.OccurredAt, doc.Type, doc.Amount);
    }
}
