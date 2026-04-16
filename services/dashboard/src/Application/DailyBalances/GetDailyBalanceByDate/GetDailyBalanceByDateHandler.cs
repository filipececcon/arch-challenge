using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Application.DailyBalances;

namespace ArchChallenge.Dashboard.Application.DailyBalances.GetDailyBalanceByDate;

public class GetDailyBalanceByDateHandler(IDailyBalanceReadStore readStore)
    : IRequestHandler<GetDailyBalanceByDateQuery, DailyBalanceDto?>
{
    public Task<DailyBalanceDto?> Handle(GetDailyBalanceByDateQuery request, CancellationToken cancellationToken) =>
        readStore.GetByDateAsync(request.Date, cancellationToken);
}
