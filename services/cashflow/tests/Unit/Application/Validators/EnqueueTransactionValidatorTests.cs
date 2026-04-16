using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application.Validators;

public class EnqueueTransactionValidatorTests
{
    private readonly EnqueueTransactionValidator _validator;

    public EnqueueTransactionValidatorTests()
    {
        var localizer = Substitute.For<IStringLocalizer<Messages>>();
        localizer[Arg.Any<string>()].Returns(x => new LocalizedString((string)x[0], (string)x[0]));

        _validator = new EnqueueTransactionValidator(localizer);
    }

    [Theory]
    [InlineData(TransactionType.Credit)]
    [InlineData(TransactionType.Debit)]
    public void Validate_WithValidType_ShouldNotHaveError(TransactionType type)
    {
        var command = BuildCommand(type: type);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Fact]
    public void Validate_WithInvalidType_ShouldHaveError()
    {
        var command = BuildCommand(type: (TransactionType)99);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage(MessageKeys.Validation.TransactionTypeInvalid);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(9999.99)]
    public void Validate_WithPositiveAmount_ShouldNotHaveError(decimal amount)
    {
        var command = BuildCommand(amount: amount);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithZeroOrNegativeAmount_ShouldHaveError(decimal amount)
    {
        var command = BuildCommand(amount: amount);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage(MessageKeys.Validation.AmountGreaterThanZero);
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveError()
    {
        var command = BuildCommand(description: null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Valid description")]
    public void Validate_WithDescriptionWithin255Chars_ShouldNotHaveError(string description)
    {
        var command = BuildCommand(description: description);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithDescriptionExceeding255Chars_ShouldHaveError()
    {
        var command = BuildCommand(description: new string('x', 256));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage(MessageKeys.Validation.DescriptionMaxLength);
    }

    [Fact]
    public void Validate_WithDescriptionExactly255Chars_ShouldNotHaveError()
    {
        var command = BuildCommand(description: new string('x', 255));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WithAllValid_ShouldPassWithoutErrors()
    {
        var command = BuildCommand();

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }

    private static EnqueueTransaction BuildCommand(
        TransactionType type        = TransactionType.Credit,
        decimal         amount      = 100m,
        string?         description = "Test transaction")
        => new(type, amount, description);
}
