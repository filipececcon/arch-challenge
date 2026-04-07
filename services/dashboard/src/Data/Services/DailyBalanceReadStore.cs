using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Application.DailyBalances;
using ArchChallenge.Dashboard.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace ArchChallenge.Dashboard.Data.Services;

public class DailyBalanceReadStore(DashboardDbContext db) : IDailyBalanceReadStore
{
    public async Task<DailyBalanceDto?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var row = await db.DailyConsolidations
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Date == date, cancellationToken);

        return row is null
            ? null
            : new DailyBalanceDto(row.Date, row.TotalCredits, row.TotalDebits, row.TotalCredits - row.TotalDebits);
    }

    public async Task<IReadOnlyList<DailyBalanceDto>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = db.DailyConsolidations.AsNoTracking().AsQueryable();

        if (from is not null)
            query = query.Where(d => d.Date >= from);
        if (to is not null)
            query = query.Where(d => d.Date <= to);

        var rows = await query.OrderBy(d => d.Date).ToListAsync(cancellationToken);

        return rows.Select(r => new DailyBalanceDto(r.Date, r.TotalCredits, r.TotalDebits, r.TotalCredits - r.TotalDebits))
            .ToList();
    }
}
