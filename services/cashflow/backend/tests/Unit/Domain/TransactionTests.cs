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

        var result = Transaction.Create(type, amount, description);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBeEmpty();
        result.Value.Type.Should().Be(type);
        result.Value.Amount.Should().Be(amount);
        result.Value.Description.Should().Be(description);
        result.Value.Active.Should().BeTrue();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldReturnFailureWithAmountError()
    {
        var result = Transaction.Create(TransactionType.Debit, 0);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Field == "Amount" && e.Message.Contains("amount"));
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldReturnFailure()
    {
        var result = Transaction.Create(TransactionType.Debit, -10m);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Field == "Amount");
    }

    [Fact]
    public void Create_WithDescriptionExceeding255Chars_ShouldReturnFailureWithDescriptionError()
    {
        var longDescription = new string('x', 256);

        var result = Transaction.Create(TransactionType.Credit, 10m, longDescription);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Field == "Description");
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ShouldReturnAllErrors()
    {
        var longDescription = new string('x', 256);

        var result = Transaction.Create(TransactionType.Debit, -5m, longDescription);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Field == "Amount");
        result.Errors.Should().Contain(e => e.Field == "Description");
    }

    [Fact]
    public void Create_ShouldRaiseTransactionRegisteredEvent()
    {
        var result = Transaction.Create(TransactionType.Credit, 200m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Events.Should().HaveCount(1);
        result.Value.Events[0].TransactionId.Should().Be(result.Value.Id);
        result.Value.Events[0].EventType.Should().Be("TransactionRegistered");
    }

    [Fact]
    public void Deactivate_ActiveTransaction_ShouldDeactivate()
    {
        var transaction = Transaction.Create(TransactionType.Debit, 50m).Value!;

        transaction.Deactivate();

        transaction.Active.Should().BeFalse();
        transaction.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_InactiveTransaction_ShouldActivate()
    {
        var transaction = Transaction.Create(TransactionType.Credit, 100m).Value!;
        transaction.Deactivate();

        transaction.Activate();

        transaction.Active.Should().BeTrue();
    }

    [Fact]
    public void ClearEvents_ShouldRemoveAllEvents()
    {
        var transaction = Transaction.Create(TransactionType.Credit, 100m).Value!;

        transaction.ClearEvents();

        transaction.Events.Should().BeEmpty();
    }
}
