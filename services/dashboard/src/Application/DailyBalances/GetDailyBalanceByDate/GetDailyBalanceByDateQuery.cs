using ArchChallenge.Dashboard.Application.DailyBalances;

namespace ArchChallenge.Dashboard.Application.DailyBalances.GetDailyBalanceByDate;

public record GetDailyBalanceByDateQuery(DateOnly Date) : IRequest<DailyBalanceDto?>;
