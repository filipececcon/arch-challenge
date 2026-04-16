using ArchChallenge.Dashboard.Application.Abstractions;
using ArchChallenge.Dashboard.Application.DailyBalances;

namespace ArchChallenge.Dashboard.Application.DailyBalances.ListDailyBalances;

public class ListDailyBalancesHandler(IDailyBalanceReadStore readStore)
    : IRequestHandler<ListDailyBalancesQuery, IReadOnlyList<DailyBalanceDto>>
{
    public Task<IReadOnlyList<DailyBalanceDto>> Handle(ListDailyBalancesQuery request, CancellationToken cancellationToken) =>
        readStore.ListAsync(request.From, request.To, cancellationToken);
}
