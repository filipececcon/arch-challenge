using ArchChallenge.Dashboard.Application.DailyBalances;

namespace ArchChallenge.Dashboard.Application.DailyBalances.ListDailyBalances;

public record ListDailyBalancesQuery(DateOnly? From, DateOnly? To) : IRequest<IReadOnlyList<DailyBalanceDto>>;
