using ArchChallenge.CashFlow.Domain.Shared.Pagination;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_Create_ShouldSetAllProperties()
    {
        var items  = new List<string> { "A", "B", "C" };
        var result = new PagedResult<string>(items, 10, 1, 3);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(3);
    }

    [Fact]
    public void TotalPages_ShouldRoundUp()
    {
        var result = new PagedResult<string>([], 10, 1, 3);

        result.TotalPages.Should().Be(4);
    }

    [Fact]
    public void TotalPages_WhenExactDivision_ShouldNotRoundUp()
    {
        var result = new PagedResult<string>([], 9, 1, 3);

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void HasNextPage_WhenMorePagesExist_ShouldBeTrue()
    {
        var result = new PagedResult<string>([], 10, 1, 3);

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_WhenOnLastPage_ShouldBeFalse()
    {
        var result = new PagedResult<string>([], 9, 3, 3);

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenOnFirstPage_ShouldBeFalse()
    {
        var result = new PagedResult<string>([], 10, 1, 3);

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenOnPageTwo_ShouldBeTrue()
    {
        var result = new PagedResult<string>([], 10, 2, 3);

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Empty_ShouldReturnResultWithZeroCountAndEmptyItems()
    {
        var result = PagedResult<string>.Empty(1, 10);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }
}
