namespace ArchChallenge.Dashboard.Application.DailyBalances;

public record DailyBalanceDto(
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal Balance);
