using System.Linq.Expressions;
using ArchChallenge.CashFlow.Domain.Shared.Criteria;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class QueryCriteriaBuilderTests
{
    private record Item(string Name, decimal Amount, bool Active);

    [Fact]
    public void Build_WithNoCriteria_ShouldReturnNull()
    {
        var builder = new QueryCriteriaBuilder<Item>();

        var result = builder.Build();

        result.Should().BeNull();
    }

    [Fact]
    public void Where_ShouldApplyPredicate()
    {
        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Active)
            .Build();

        predicate.Should().NotBeNull();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, false)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().ContainSingle(x => x.Name == "A");
    }

    [Fact]
    public void AndIf_WhenConditionTrue_ShouldApplyPredicate()
    {
        decimal minAmount = 15m;

        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Active)
            .AndIf(true, x => x.Amount >= minAmount)
            .Build();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, true),
            new Item("C", 5m,  false)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().ContainSingle(x => x.Name == "B");
    }

    [Fact]
    public void AndIf_WhenConditionFalse_ShouldNotApplyPredicate()
    {
        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Active)
            .AndIf(false, x => x.Amount >= 100m)
            .Build();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, false)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().ContainSingle(x => x.Name == "A");
    }

    [Fact]
    public void Or_ShouldCombineWithOrLogic()
    {
        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Name == "A")
            .Or(x => x.Name == "C")
            .Build();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, true),
            new Item("C", 30m, false)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().HaveCount(2);
        filtered.Select(x => x.Name).Should().BeEquivalentTo(new[] { "A", "C" });
    }

    [Fact]
    public void OrIf_WhenConditionTrue_ShouldApplyOrPredicate()
    {
        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Name == "A")
            .OrIf(true, x => x.Name == "B")
            .Build();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, true),
            new Item("C", 30m, false)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void OrIf_WhenConditionFalse_ShouldNotApplyOrPredicate()
    {
        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Name == "A")
            .OrIf(false, x => x.Name == "B")
            .Build();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, true)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().ContainSingle(x => x.Name == "A");
    }

    [Fact]
    public void Where_MultipleTimes_ShouldApplyAllCriteriaWithAnd()
    {
        var predicate = new QueryCriteriaBuilder<Item>()
            .Where(x => x.Active)
            .Where(x => x.Amount > 15m)
            .Build();

        var items = new[]
        {
            new Item("A", 10m, true),
            new Item("B", 20m, true),
            new Item("C", 30m, false)
        };

        var filtered = items.AsQueryable().Where(predicate!).ToList();
        filtered.Should().ContainSingle(x => x.Name == "B");
    }
}

public class PredicateBuilderTests
{
    private record Item(string Name, decimal Amount);

    [Fact]
    public void And_WithBothNull_ShouldReturnNull()
    {
        var result = PredicateBuilder.And<Item>(null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void And_WithLeftNull_ShouldReturnRight()
    {
        Expression<Func<Item, bool>> right = x => x.Amount > 10m;

        var result = PredicateBuilder.And<Item>(null, right);

        result.Should().BeSameAs(right);
    }

    [Fact]
    public void And_WithRightNull_ShouldReturnLeft()
    {
        Expression<Func<Item, bool>> left = x => x.Name == "A";

        var result = PredicateBuilder.And<Item>(left, null);

        result.Should().BeSameAs(left);
    }

    [Fact]
    public void And_WithBothProvided_ShouldCombineWithAndLogic()
    {
        Expression<Func<Item, bool>> left  = x => x.Name == "B";
        Expression<Func<Item, bool>> right = x => x.Amount > 10m;

        var combined = PredicateBuilder.And(left, right)!.Compile();

        combined(new Item("B", 20m)).Should().BeTrue();
        combined(new Item("B", 5m)).Should().BeFalse();
        combined(new Item("A", 20m)).Should().BeFalse();
    }

    [Fact]
    public void Or_WithBothNull_ShouldReturnNull()
    {
        var result = PredicateBuilder.Or<Item>(null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Or_WithBothProvided_ShouldCombineWithOrLogic()
    {
        Expression<Func<Item, bool>> left  = x => x.Name == "A";
        Expression<Func<Item, bool>> right = x => x.Name == "B";

        var combined = PredicateBuilder.Or(left, right)!.Compile();

        combined(new Item("A", 10m)).Should().BeTrue();
        combined(new Item("B", 20m)).Should().BeTrue();
        combined(new Item("C", 30m)).Should().BeFalse();
    }

    [Fact]
    public void Not_ShouldNegatePredicate()
    {
        Expression<Func<Item, bool>> expr = x => x.Amount > 10m;

        var notExpr = PredicateBuilder.Not(expr).Compile();

        notExpr(new Item("A", 5m)).Should().BeTrue();
        notExpr(new Item("A", 20m)).Should().BeFalse();
    }
}
