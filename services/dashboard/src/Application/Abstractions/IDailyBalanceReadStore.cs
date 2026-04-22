using ArchChallenge.Dashboard.Application.DailyBalances;

namespace ArchChallenge.Dashboard.Application.Abstractions;

public interface IDailyBalanceReadStore
{
    Task<DailyBalanceDto?> GetByDateAsync(DateOnly date, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyBalanceDto>> ListAsync(
        DateOnly? from,
        DateOnly? to,
        string? userId,
        CancellationToken cancellationToken);
}
