using ArchChallenge.Dashboard.Application.DailyBalances;
using Xunit;

namespace ArchChallenge.Dashboard.Tests.Unit;

public class DailyBalanceDtoTests
{
    [Fact]
    public void Balance_ShouldBeCreditsMinusDebits()
    {
        var dto = new DailyBalanceDto(new DateOnly(2026, 4, 6), 100m, 30m, 70m);
        Assert.Equal(70m, dto.Balance);
    }
}
