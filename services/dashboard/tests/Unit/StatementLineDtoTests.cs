using ArchChallenge.Dashboard.Application.Statement;
using Xunit;

namespace ArchChallenge.Dashboard.Tests.Unit;

public class StatementLineDtoTests
{
    [Fact]
    public void StatementLineDto_ShouldHoldCreditValues()
    {
        var occurred = new DateTime(2026, 4, 10, 14, 30, 0, DateTimeKind.Utc);
        var accountId = Guid.NewGuid();
        var dto = new StatementLineDto(
            Guid.NewGuid(),
            accountId,
            new DateOnly(2026, 4, 10),
            occurred,
            "CREDIT",
            250.00m);

        Assert.Equal("CREDIT", dto.Type);
        Assert.Equal(250.00m, dto.Amount);
        Assert.Equal(new DateOnly(2026, 4, 10), dto.Date);
        Assert.Equal(accountId, dto.AccountId);
    }

    [Fact]
    public void StatementPageDto_ShouldExposeCorrectTotalCount()
    {
        var aid = Guid.NewGuid();
        var lines = new List<StatementLineDto>
        {
            new(Guid.NewGuid(), aid, new DateOnly(2026, 4, 9), DateTime.UtcNow, "DEBIT", 100m),
            new(Guid.NewGuid(), aid, new DateOnly(2026, 4, 9), DateTime.UtcNow, "CREDIT", 200m),
        };

        var page = new StatementPageDto(lines, TotalCount: 50, Page: 1, PageSize: 2);

        Assert.Equal(50, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Page);
    }
}
