using ArchChallenge.CashFlow.Application.Transactions.Queries.GetAllTransactions;
using ArchChallenge.CashFlow.Infrastructure.CrossCutting.I18n;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application.Validators;

public class GetAllTransactionsQueryValidatorTests
{
    private readonly GetAllTransactionsQueryValidator _validator;

    public GetAllTransactionsQueryValidatorTests()
    {
        var localizer = Substitute.For<IStringLocalizer<Messages>>();
        localizer[Arg.Any<string>()].Returns(x => new LocalizedString((string)x[0], (string)x[0]));

        _validator = new GetAllTransactionsQueryValidator(localizer);
    }

    [Fact]
    public void Validate_WithNoFilters_ShouldPassWithoutErrors()
    {
        var query = new GetAllTransactionsQuery();

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Credit")]
    [InlineData("Debit")]
    [InlineData("credit")]
    [InlineData("DEBIT")]
    public void Validate_WithValidType_ShouldNotHaveError(string type)
    {
        var query = new GetAllTransactionsQuery(Type: type);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Type);
    }

    [Theory]
    [InlineData("Transfer")]
    [InlineData("invalid")]
    [InlineData("Credito")]
    public void Validate_WithInvalidType_ShouldHaveError(string type)
    {
        var query = new GetAllTransactionsQuery(Type: type);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Type)
            .WithErrorMessage(MessageKeys.Validation.TransactionTypeInvalid);
    }

    [Fact]
    public void Validate_WithNullType_ShouldNotValidateType()
    {
        var query = new GetAllTransactionsQuery(Type: null);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMinAmountLessThanMaxAmount_ShouldNotHaveError()
    {
        var query = new GetAllTransactionsQuery(MinAmount: 10m, MaxAmount: 100m);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.MaxAmount);
    }

    [Fact]
    public void Validate_WithMinAmountEqualToMaxAmount_ShouldNotHaveError()
    {
        var query = new GetAllTransactionsQuery(MinAmount: 50m, MaxAmount: 50m);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.MaxAmount);
    }

    [Fact]
    public void Validate_WithMinAmountGreaterThanMaxAmount_ShouldHaveError()
    {
        var query = new GetAllTransactionsQuery(MinAmount: 200m, MaxAmount: 100m);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == MessageKeys.Validation.GetAllAmountRange);
    }

    [Fact]
    public void Validate_WithOnlyMinAmount_ShouldNotValidateRange()
    {
        var query = new GetAllTransactionsQuery(MinAmount: 100m);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithOnlyMaxAmount_ShouldNotValidateRange()
    {
        var query = new GetAllTransactionsQuery(MaxAmount: 100m);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCreatedFromBeforeCreatedTo_ShouldNotHaveError()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to   = DateTime.UtcNow;

        var query = new GetAllTransactionsQuery(CreatedFrom: from, CreatedTo: to);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.CreatedTo);
    }

    [Fact]
    public void Validate_WithCreatedFromEqualToCreatedTo_ShouldNotHaveError()
    {
        var date  = DateTime.UtcNow;
        var query = new GetAllTransactionsQuery(CreatedFrom: date, CreatedTo: date);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.CreatedTo);
    }

    [Fact]
    public void Validate_WithCreatedFromAfterCreatedTo_ShouldHaveError()
    {
        var from  = DateTime.UtcNow;
        var to    = DateTime.UtcNow.AddDays(-1);
        var query = new GetAllTransactionsQuery(CreatedFrom: from, CreatedTo: to);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == MessageKeys.Validation.GetAllCreatedAtRange);
    }

    [Fact]
    public void Validate_WithOnlyCreatedFrom_ShouldNotValidateDateRange()
    {
        var query = new GetAllTransactionsQuery(CreatedFrom: DateTime.UtcNow);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeTrue();
    }
}
