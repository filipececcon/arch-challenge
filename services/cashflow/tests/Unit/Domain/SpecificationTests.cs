using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Domain.Shared.Entities;
using ArchChallenge.CashFlow.Domain.Shared.Specifications;
using ArchChallenge.CashFlow.Domain.Specifications;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class SpecificationTests
{
    [Fact]
    public void TransactionByIdSpec_ShouldHaveCriteriaFilteringById()
    {
        var id   = Guid.NewGuid();
        var spec = new TransactionByIdSpec(id);

        spec.Criteria.Should().NotBeNull();
    }

    [Fact]
    public void TransactionByIdSpec_Criteria_ShouldMatchOnlyTheCorrectId()
    {
        var id       = Guid.NewGuid();
        var spec     = new TransactionByIdSpec(id);
        var compiled = spec.Criteria!.Compile();

        var matching    = new Transaction(TransactionType.Credit, 100m);
        var notMatching = new Transaction(TransactionType.Credit, 100m);

        typeof(Entity).GetProperty("Id")!.SetValue(matching, id);

        compiled(matching).Should().BeTrue();
        compiled(notMatching).Should().BeFalse();
    }

    [Fact]
    public void TransactionsOrderedByDateSpec_ShouldHaveOrderByDescending()
    {
        var spec = new TransactionsOrderedByDateSpec();

        spec.OrderByDescending.Should().NotBeNull("a spec deve ter ordenação descendente por CreatedAt");
        spec.Criteria.Should().BeNull("a spec não deve ter filtro, apenas ordenação");
    }

    [Fact]
    public void Specification_AddInclude_ShouldAddToIncludesList()
    {
        var spec = new TestSpec();
        spec.CallAddInclude(t => t.Amount);

        spec.Includes.Should().HaveCount(1);
    }

    [Fact]
    public void Specification_AddOrderBy_ShouldSetOrderBy()
    {
        var spec = new TestSpec();
        spec.CallAddOrderBy(t => t.Amount);

        spec.OrderBy.Should().NotBeNull();
    }

    [Fact]
    public void Specification_ApplyPaging_ShouldSetSkipAndTake()
    {
        var spec = new TestSpec();
        spec.CallApplyPaging(skip: 10, take: 5);

        spec.Skip.Should().Be(10);
        spec.Take.Should().Be(5);
        spec.IsPagingEnabled.Should().BeTrue();
    }

    [Fact]
    public void Specification_WithoutPaging_IsPagingEnabledShouldBeFalse()
    {
        var spec = new TestSpec();

        spec.IsPagingEnabled.Should().BeFalse();
    }

    // Helper que expõe os métodos protegidos da Specification base para teste
    private sealed class TestSpec : Specification<Transaction>
    {
        public void CallAddInclude(System.Linq.Expressions.Expression<Func<Transaction, object>> expr)
            => AddInclude(expr);

        public void CallAddOrderBy(System.Linq.Expressions.Expression<Func<Transaction, object>> expr)
            => AddOrderBy(expr);

        public void CallApplyPaging(int skip, int take)
            => ApplyPaging(skip, take);
    }
}
