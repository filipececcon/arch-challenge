using ArchChallenge.Dashboard.Application.DailyBalances;
using MediatR;

namespace ArchChallenge.Dashboard.Application.DailyBalances.GetDailyBalanceByDate;

public record GetDailyBalanceByDateQuery(DateOnly Date) : IRequest<DailyBalanceDto?>;
