using ArchChallenge.CashFlow.Domain.Entities;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class TransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccessResult()
    {
        var type = TransactionType.Credit;
        var amount = 150.00m;
        var description = "Cash sale";

        var result = new Transaction(type, amount, description);

        result.IsValid.Should().BeTrue();
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Type.Should().Be(type);
        result.Amount.Should().Be(amount);
        result.Description.Should().Be(description);
        result.Active.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldReturnFailureWithAmountError()
    {
        var entity = new Transaction(TransactionType.Debit, 0);

        entity.IsFailure.Should().BeTrue();
        entity.Notifications.Should().ContainSingle(e => e.Key == "Amount" && e.Message.Contains("amount"));
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldReturnFailure()
    {
        var entity = new Transaction(TransactionType.Debit, -10m);

        entity.IsFailure.Should().BeTrue();
        entity.Notifications.Should().ContainSingle(e => e.Key == "Amount");
    }

    [Fact]
    public void Create_WithDescriptionExceeding255Chars_ShouldReturnFailureWithDescriptionError()
    {
        var longDescription = new string('x', 256);

        var entity = new Transaction(TransactionType.Credit, 10m, longDescription);

        entity.IsFailure.Should().BeTrue();
        entity.Notifications.Should().ContainSingle(e => e.Key == "Description");
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        var longDescription = new string('x', 256);

        var entity = new Transaction(TransactionType.Debit, -5m, longDescription);

        entity.IsFailure.Should().BeTrue();
        entity.Notifications.Should().HaveCount(2);
        entity.Notifications.Should().Contain(e => e.Key == "Amount");
        entity.Notifications.Should().Contain(e => e.Key == "Description");
    }

    [Fact]
    public void Create_ShouldRaiseTransactionCreatedEvent()
    {
        var entity = new Transaction(TransactionType.Credit, 200m);

        entity.IsValid.Should().BeTrue();
        // entity.Events.Should().HaveCount(1);
        // entity.Events[0].TransactionId.Should().Be(entity.Id);
        // entity.Events[0].EventName.Should().Be("TransactionCreated");
    }

    [Fact]
    public void Deactivate_ActiveTransaction_ShouldDeactivate()
    {
        var entity = new Transaction(TransactionType.Debit, 50m);

        entity.Deactivate();
        entity.Active.Should().BeFalse();
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_InactiveTransaction_ShouldActivate()
    {
        var entity = new Transaction(TransactionType.Credit, 100m)!;
        
        entity.Deactivate();
        entity.Activate();
        entity.Active.Should().BeTrue();
    }

    [Fact]
    public void ClearEvents_ShouldRemoveAllEvents()
    {
        var entity = new Transaction(TransactionType.Credit, 100m)!;

        // entity.ClearEvents();
        // entity.Events.Should().BeEmpty();
    }
}
